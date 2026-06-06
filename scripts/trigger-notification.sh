#!/usr/bin/env bash
# Dev helper: make a seeded profile appreciate another, which fires a
# received-appreciation notification (toast + Received dot) for the receiver.
#
# Usage:
#   scripts/trigger-notification.sh                       # auto-pick a fresh sender+trait
#   scripts/trigger-notification.sh SENDER_EMAIL TRAIT    # explicit (may 409 if already used)
#   scripts/trigger-notification.sh SENDER_EMAIL TRAIT RECEIVER_EMAIL
#
# Default receiver: test@notice.local
#
# A sender can appreciate a given receiver only ONCE per category, so the no-arg
# mode auto-selects a (sender, category) pair that hasn't been used yet — every
# run creates a genuinely NEW unread reaction (HTTP 201), so the red dot reliably
# appears. When every combo is exhausted it says so.
set -euo pipefail

API=http://localhost:5080/api/v1
MAILPIT=http://localhost:18025
PSQL=(docker exec -e PGPASSWORD=hpn hpn-postgres-1 psql -U hpn -d hpn -tA)

RECEIVER="${3:-test@notice.local}"
RECV_PROFILE=$("${PSQL[@]}" -c "SELECT p.id FROM profile.profiles p JOIN identity.users u ON u.id=p.user_id WHERE u.email='$RECEIVER';")
[ -n "$RECV_PROFILE" ] || { echo "no profile for $RECEIVER"; exit 1; }

if [ -n "${1:-}" ]; then
  SENDER="$1"
  TRAIT="${2:?need a trait slug when sender is given}"
else
  # Auto-pick a random active seed sender + a trait from a category they have NOT
  # yet used on this receiver. Guarantees a 201 (new unread reaction).
  read -r SENDER TRAIT < <("${PSQL[@]}" -c "
    SELECT u.email, t.slug
    FROM identity.users u
    JOIN profile.profiles sp ON sp.user_id = u.id AND sp.status = 'active'
    JOIN appreciation.appreciation_categories c ON TRUE
    JOIN appreciation.appreciation_traits t ON t.category_id = c.id AND t.active
    WHERE u.email LIKE 'seed-candidate-%@notice.local'
      AND u.id <> (SELECT user_id FROM profile.profiles WHERE id='$RECV_PROFILE')
      AND NOT EXISTS (
        SELECT 1 FROM appreciation.appreciation_events e
        WHERE e.sender_user_id = u.id
          AND e.receiver_profile_id = '$RECV_PROFILE'
          AND e.category_id = c.id)
    ORDER BY random() LIMIT 1;" | tr '|' ' ')
  [ -n "${SENDER:-}" ] || { echo "no unused sender/category combos left for $RECEIVER (every seed sender has appreciated them in every category)"; exit 2; }
fi

# Log the sender in via magic link + Mailpit.
curl -s -o /dev/null -X POST "$API/auth/magic-link" -H 'Content-Type: application/json' -d "{\"email\":\"$SENDER\"}"
MID=$(curl -s "$MAILPIT/api/v1/search?query=$SENDER&limit=1" | grep -oP '"ID":"\K[^"]+' | head -1)
TOKEN=$(curl -s "$MAILPIT/api/v1/message/$MID" | grep -oP 'token=\K[A-Za-z0-9_\-%]+' | head -1 \
        | python3 -c 'import sys,urllib.parse;print(urllib.parse.unquote(sys.stdin.read().strip()))')
COOKIE=$(curl -s -D - -o /dev/null -X POST "$API/auth/verify" -H 'Content-Type: application/json' \
        -d "{\"token\":\"$TOKEN\"}" | grep -oiP 'set-cookie: \Khpn_session=[^;]+' | head -1)
TID=$(curl -s -H "Cookie: $COOKIE" "$API/appreciation-categories" \
      | python3 -c "import sys,json;print(next(t['id'] for c in json.load(sys.stdin) for t in c.get('traits',[]) if t['slug']=='$TRAIT'))")
CODE=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/appreciations" -H "Cookie: $COOKIE" \
      -H 'Content-Type: application/json' -H "Idempotency-Key: trigger-$(date +%s%N)" \
      -d "{\"receiverProfileId\":\"$RECV_PROFILE\",\"traitId\":\"$TID\",\"photoId\":null}")
echo "$SENDER --($TRAIT)--> $RECEIVER : HTTP $CODE  (201=new unread reaction, 409=already that category)"
