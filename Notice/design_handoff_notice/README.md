# Handoff: Notice — Appreciation-First Dating PWA (Redesign)

## Overview

**Notice** (codename HPN, "Human Perception Network") is an appreciation-first social/dating app. Unlike a swipe app, there is **no reject, skip, or dislike** — the *only* way to move past a profile is to genuinely appreciate something specific about that person. The app is a mobile **PWA** and must look and behave like a native app.

This package is a full redesign covering five surfaces:

1. **Onboarding / Auth** — entry that doubles as sign-in.
2. **Feed** — the core loop: one profile at a time → appreciate → next.
3. **Received** — the kind words people have chosen about you (private).
4. **Fingerprint** — a radar "perception shape" built from how others appreciate you.
5. **You** — profile + privacy settings.

Plus a scripted **first-time emotional moment** (a new member receives their first appreciation).

---

## About the Design Files

The files in this bundle are **design references created in HTML/React (via in-browser Babel)** — they are prototypes that demonstrate the intended look, motion, and behavior. **They are not production code to copy directly.**

Your task is to **recreate these designs in the target codebase's existing environment**. The original product is a **.NET backend + a Vite/React/TypeScript frontend** (`web/notice/`, using React Router, TanStack Query, and an OpenAPI-generated client). Implement these screens as React/TS components that fit the existing architecture and reuse its data layer (the API hooks for feed, appreciation, profile, settings already exist). Do **not** ship the HTML.

### How to read the bundle
- `Notice.html` — entry point; wires fonts, scripts, and mounts the app.
- `app/data.jsx` — **the source of truth for the reaction taxonomy, colors, and mock content.** Read this first.
- `app/ui.jsx` — shared atoms (wordmark, nav icons, monogram portrait placeholder).
- `app/feed.jsx` — the feed card + appreciate FAB + trait cloud + reward animations.
- `app/panels.jsx` — header, bottom nav, Received (established), Fingerprint (established + radar), locked states.
- `app/onboarding.jsx` — auth/sign-in + multi-step profile setup + You screen.
- `app/journey.jsx` — first-time moment: incoming toast, fresh/empty/reveal Received, nascent Fingerprint, count-up hook.
- `app/main.jsx` — app root: state machine (anon/member, fresh/established), routing, tweak controls.
- `ios-frame.jsx`, `tweaks-panel.jsx`, `image-slot.js` — **prototype scaffolding only.** The iOS bezel, the live "Tweaks" panel, and the drag-drop image slot are demo affordances. **Do not port them** — they are not part of the product.

---

## Fidelity

**High-fidelity (hifi).** Final colors, typography, spacing, motion, and copy are all specified here and in the CSS (`app/styles.css`). Recreate pixel-for-pixel using the codebase's existing component library and patterns. Where this design and the existing design system disagree on a primitive (e.g. button base), prefer the codebase's primitive but match these visual specs (color, radius, type).

**Design canvas:** 402 × 874 px (logical iPhone viewport). All values below are at 1× for that width. The app is a single full-height column; it does not scroll horizontally. Only the inner content scrolls.

---

## Design Tokens

Defined as CSS custom properties in `app/styles.css` `:root`. Port these into the codebase's token system.

### Colors
| Token | Value | Use |
|---|---|---|
| `--cream` | `#f4eee3` | App background (canvas) |
| `--cream-2` | `#ece4d6` | Inset/track fills, pressed states |
| `--paper` | `#fffdf9` | Cards, sheets, inputs (warm white) |
| `--paper-2` | `#fbf6ed` | Subtle panel fill, image-slot bg |
| `--ink` | `#2a2520` | Primary text (warm near-black) |
| `--ink-2` | `#6e655a` | Secondary text |
| `--ink-3` | `#a89d8e` | Tertiary text, inactive icons |
| `--line` | `rgba(42,37,32,0.08)` | Hairline borders |
| `--line-strong` | `rgba(42,37,32,0.16)` | Stronger borders, input outlines |
| `--coral` | `oklch(0.71 0.14 41)` | Primary accent (≈ `#e0764f`) |
| `--coral-deep` | `oklch(0.57 0.15 38)` | Active text/icon accent (≈ `#b9542f`) |
| `--coral-soft` | `oklch(0.95 0.045 45)` | Soft accent fill (≈ `#f7e6dd`) |

> The primary is a **friendly warm coral**, deliberately *not* Tinder hot-pink/red.

### Category accent system (the 6 reaction categories)
Each category has a single **hue** on a shared OKLCH lightness/chroma, giving a harmonious set. This is how a reaction's category is conveyed **by color alone** in the flattened picker. Helpers in `data.jsx`:
- Accent dot/spike: `cat(hue)` = `oklch(0.70 0.13 <hue>)`
- Soft chip fill: `catSoft(hue)` = `oklch(0.95 0.045 <hue>)`
- Tint: `catTint(hue)` = `oklch(0.97 0.03 <hue>)`
- Readable ink-on-soft: `catInk(hue)` = `oklch(0.46 0.11 <hue>)`

| Category | id | hue | Reads as |
|---|---|---|---|
| Physical | `physical` | 38 | coral/amber |
| Energy | `energy` | 78 | amber/gold |
| Style | `style` | 350 | rose/pink |
| Humor | `humor` | 142 | green |
| Mind | `mind` | 264 | periwinkle/violet |
| Authentic | `authentic` | 200 | teal/blue |

### Typography
- **Display / headings:** `Bricolage Grotesque` (Google Fonts, opsz 12–96, wght 400–800). Used for the wordmark, names, screen `<h1>`, card titles, buttons. Characterful, warm.
- **Body / UI:** `Hanken Grotesk` (Google Fonts, wght 400–700). Used for body copy, meta, chips.
- **Mono:** system mono (`ui-monospace, SFMono-Regular, Menlo`). Used only for tiny labels (e.g. "PORTRAIT" placeholder tag, "NO SHAPE YET").

Type scale (px / weight / line-height):
| Role | Size | Weight | Notes |
|---|---|---|---|
| Screen H1 (lead) | 27 | 700 | Bricolage, letter-spacing −0.015em, `text-wrap: balance` |
| Welcome/auth H1 | 34 | 700 | Bricolage, letter-spacing −0.02em |
| Onboarding step H2 | 25 | 700 | Bricolage |
| Card name | 29–35 | 700 | Bricolage; size varies by density (see Feed) |
| Card title / first-card | 20 | 700 | Bricolage |
| Section header ("block-h") | 11 | 700 | uppercase, letter-spacing 0.14em, color `--ink-3` |
| Eyebrow | 11 | 700 | uppercase, letter-spacing 0.18em, color `--coral-deep` |
| Body / lead-sum | 13.5 | 400–500 | line-height 1.5, color `--ink-2` |
| Chips / trait | 14 | 700 | pill |
| Nav label | 10.5 | 600 | |

### Spacing, radius, shadow
- Screen horizontal padding: **22px** (scroll screens), feed card margin **6/14/22px** by density.
- Card radius: **24px default** (tunable 8–34 in prototype; ship 24).
- Other radii: sheets 26px, tray 22px, inputs 13px, segmented buttons 12px, pills/chips 999px, FAB 50%.
- Card shadow: `0 18px 44px -16px rgba(42,37,32,0.36), 0 2px 6px rgba(42,37,32,0.06)`.
- Primary button shadow: `0 10px 22px -8px oklch(0.62 0.16 40 / 0.7), inset 0 1px 0 rgba(255,255,255,0.3)`.
- Toast shadow: `0 16px 38px -12px rgba(42,37,32,0.34)`.

---

## The Reaction Taxonomy (critical — see `app/data.jsx`)

6 categories, each with specific traits. **In the current design the picker is flattened**: the user sees all traits at once as a single color-coded cloud (category = color), so reacting is **one tap to open, one tap to choose**. The category grouping is retained in data (for color + analytics) but is **not** a navigation level.

```
physical (hue 38):  Warm smile · Kind eyes · Great hair · Natural glow
energy   (hue 78):  Good vibe · Confident · Calm presence · Magnetic
style    (hue 350): Great fit · Effortless · Signature look
humor    (hue 142): Made me grin · Quick wit · Wonderfully odd
mind     (hue 264): Curious · Thoughtful · Sharp
authentic(hue 200): Genuine · Grounded · True to themselves
```
`ALL_TRAITS` is the flattened list (`{label, category, hue}`), rendered in category order so colors cluster like a soft rainbow. The backend appreciation API already models category + specific trait — map these labels to its enum.

---

## Screens / Views

### 1. App chrome (present on all logged-in tabs)
- **Header** (`.app-header`): top padding 58px (status bar safe area), 22px sides. Left: **wordmark** = a small ring with a filled coral dot ("a noticing eye") + "Notice" in Bricolage 17/700. Right slot is conditional:
  - **Anonymous users:** a pulsing **nudge pill** — `--coral-soft` fill, `--coral-deep` text, person-plus icon, label "Be noticed back", gentle bob + halo pulse. Tapping opens the Auth overlay.
  - **Members:** a **gear** ghost button → opens You.
- **Bottom nav** (`.bottom-nav`): 4 tabs — **Notice** (feed), **Received**, **Fingerprint**, **You**. Paper bg with blur, top hairline, bottom padding 26px (home-indicator safe area). Active = `--coral-deep`; inactive = `--ink-3`. Icons are simple line glyphs (see `Icon` in `ui.jsx`). Tap scales 0.92.

### 2. Feed (core loop) — `app/feed.jsx`
The landing surface. Anonymous users browse immediately (no gate).

- **Card** (`.card-front`): full-bleed photo filling the card; rounded 24px; margin from edges set by **density** tweak (compact 6 / regular 14 / comfy 22 — ship "regular"). A second card (`.card-behind`, next profile) sits behind, scaled 0.95, translateY 12px, opacity 0.55 — for depth.
- **On the photo:**
  - Bottom scrim gradient (`to top, rgba(28,22,16,0.86) → transparent` at 52%).
  - Identity (bottom-left, padding 20, right padding 78 to clear FAB): **Name** (Bricolage 31/700, white) + a **tiny, low-opacity gender glyph** beside it (`♀ ♂ ⚧ ·` for woman/man/nonbinary/undisclosed, opacity 0.55). Then a wrapping row of **interest chips** (white translucent pills, 11.5/600, `backdrop-filter: blur(6px)`).
  - **No country, no distance, no bio** on the card. Just name, glyph, interests.
  - **Report button** (top-right, `.report-btn`): 30px circle, very low opacity (0.42), translucent dark bg + blur — intentionally *second-plane* so it never competes with the appreciate flow. Tap → turns into a green check ("Reported — thank you").
  - **Appreciate FAB** (bottom-right, `.fab`): 60px coral gradient circle with a "+" glyph and a **continuous invite pulse** (expanding ring shadow, `fabPulse` 2.4s). This is the *only* primary action. When open it rotates 90° into an "×" (close).
- **The trait cloud** (the reaction picker): two presentations (tweak `flow`, ship **tray**):
  - **tray:** a glass panel (`backdrop-filter: blur(16px)`) springs up anchored above the FAB (transform-origin bottom-right). Header "Appreciate something real", then all 20 traits as color-coded pills (`catSoft` fill, `catInk` text). One tap on a pill = the reaction.
  - **sheet:** same content in a full-width bottom sheet with a grip handle and scrim.
- **Reward moment** (on choosing a trait — tweak `reward`, ship **both**):
  1. Haptic (`navigator.vibrate(12)`).
  2. Photo **pulse** (scale to 1.022 and back, 0.56s).
  3. Expanding **glow ring** in the trait's category color (`glow` keyframe).
  4. **Confetti** burst (18 particles, category-hued).
  5. The chosen **trait label floats up** (`trait-float`, rises and fades).
  6. Card **flies away** (translate up-left, rotate −8°, fade) and the next card springs in.
  Total sequence ≈ 1.02s, then index advances. Honor `prefers-reduced-motion`.
- **Hint line** under the card: "The only way forward is to appreciate." → "Tap a word you mean." when open.

### 3. Received (established member) — `ReceivedScreen` in `panels.jsx`
Private collection of the words people chose. Scrollable.
- **Lead:** eyebrow "Received", H1 "Quietly, people keep noticing you.", summary, and a private count line ("34 appreciations received privately.").
- **"Ways people describe you":** 2-col grid of cards. Each card: category dot, trait label (Bricolage 14.5/600), a **count badge** (soft category fill), and a perception phrasing ("People often notice your warm smile.").
- **"Recent notes":** list rows, each a category dot + phrasing + relative time.

### 4. Fingerprint (established member) — `FingerprintScreen` + `Radar` in `panels.jsx`
- **Lead:** eyebrow, H1 "People meet you as warm and grounded.", summary phrased as *perception, never a score*, private count.
- **"Perception shape":** a 6-axis **radar/spider chart** (one axis per category), filled coral polygon (`oklch(0.7 0.13 38 / 0.16)` fill, `--coral` stroke), with a colored vertex dot per category and external labels. Built as inline SVG — see `Radar`; it takes a `distribution` array of `{category, share}`.
- **"Recurring traits":** list of top traits, each with a category dot, label, **percentage**, a progress bar in the category color, and a phrasing.
- **"Distribution":** all categories sorted by share with bars + percentages.

### 5. You — `YouScreen` in `onboarding.jsx`
- **Anonymous:** "You're appreciating anonymously." + a "Create my profile" CTA (no fingerprint yet).
- **Member:** "Your profile is active." Reinforces what is *never* shown: no age, height, body type, income, scores, or public counts.
  - **"Who can see you"** toggles: Take a break · **Women appreciating women only** · **Hide me from people in my own country** · **Only show me people outside my country** · Only connect with verified people.
  - **Data/account** rows: Edit profile · Download my data · Blocked people · Sign out · Delete account.

### 6. Onboarding / Auth — `OnboardingFlow` in `onboarding.jsx`
Opens as a full-screen overlay over the feed (anon users reach it via the nudge or You).
- **Auth entry (doubles as sign-in):** close "×" (keep browsing), wordmark. H1 "Be noticed back." (or "Welcome back." in sign-in mode). Email field. Buttons: **Create my profile** / **I already have an account** (toggles to sign-in: **Sign in** / **New here? Create a profile**). Footer link: "Keep browsing anonymously."
- **Step 1 — Basics:** 3-up photo slots (real app: image upload; prototype uses drag-drop `image-slot`), display name, and **gender** as a 4-up segmented control (Woman / Man / Non-binary / Rather not say) — noted as "shown as a small, quiet glyph". Copy emphasizes: **no age, height, body type or income — ever.**
- **Step 2 — Interests:** pick up to 6 from a chip cloud.
- **Step 3 — Privacy:** the toggles from You, on by default mindset. The **"Women appreciating women only"** toggle appears **only when gender = Woman**. Footer note: location is kept **coarse (rounded to ~11 km, broad distance bands only)**, never exact.
- Stepper pips at top; back nav between steps; "Activate my profile" completes → becomes a **fresh** member.

### 7. First-time emotional moment — `app/journey.jsx`
Triggered when a user **completes profile creation** (becomes `maturity: 'fresh'`). Signing in instead → `established` (skips this, shows rich data).
1. **~2.8s after landing on the feed**, an **incoming toast** slides under the header: a category-colored check dot (with burst pulse), title "Someone just noticed your warm smile.", sub "Tap to see what they appreciated", and a dismiss ×. Auto-dismisses after 8s.
2. **Tap → Received** opens in **reveal** mode: eyebrow "Just now", H1 "Your first appreciation.", and the **first-appreciation card** pops in (scale-in) with a glow ring + confetti; the **count animates 0 → 1** (`useCountUp`); anonymity note "They didn't say who — only what they appreciated."; a "What happens next" explainer.
3. **Fingerprint** for a fresh member shows the **nascent** state: H1 "Your shape is just beginning.", a radar with a **single coral spike** (only Physical), and a **"Taking form: 1 of 20"** progress bar. Before the first appreciation, Received shows an **empty/waiting** state ("Your profile is live. Now we wait.") with a pulsing ring motif.

> Anonymous users who tap Received/Fingerprint get a **locked** state nudging signup (`LockedScreen`).

---

## Interactions & Behavior (summary)
- **Single positive action.** No skip/dislike/undo. Advancing the feed *requires* choosing a trait.
- **Reaction:** open picker (FAB) → choose trait → reward sequence (~1.02s) → next profile. Debounce so a card can't be double-reacted while animating.
- **Animations:** all entrance animations should make the **visible end-state the base** and animate *from* hidden, gated on `prefers-reduced-motion: no-preference`, so reduced-motion / SSR / print show content. Durations: card in 0.5s, fly-away 0.46s, glow 0.6–0.7s, confetti 0.9s, trait float 1.0s, toast/tray/sheet 0.34–0.5s.
- **Haptics:** fire light haptic on react (and optionally on toast open) where supported.
- **Persistence (prototype):** account/maturity/firstSeen/tab in localStorage — in production these come from auth/session + API.

## State Management
Model (see `app/main.jsx`):
- `account`: `anon | member`.
- `maturity`: `fresh | established` (member only) — drives whether Received/Fingerprint show nascent vs rich, and whether the first-time toast arms.
- `firstSeen`: whether the first appreciation has been opened (gates empty → reveal → early).
- `tab`: `feed | received | fingerprint | you`.
- transient: `authOpen`, `toast` (the incoming appreciation), `reveal` (play the first-card reveal once).
In production, replace localStorage with the existing **TanStack Query** hooks: feed pagination, submit-appreciation mutation, received list, fingerprint aggregate, profile, and settings — all already present in `web/notice/src/lib/api/`.

## Assets
- **Fonts:** Google Fonts — Bricolage Grotesque + Hanken Grotesk (swap to the codebase's font-loading mechanism).
- **Icons:** all inline SVG line icons drawn in the prototype (nav, gear, report, person-plus, chevrons, check, plus/close). Replace with the codebase's icon set, matching weight (~1.8–2.2px stroke, round caps).
- **Profile photos:** the prototype uses **monogram placeholders** (`Portrait` in `ui.jsx`) tinted per profile, tagged "PORTRAIT". Real app uses uploaded user photos. No production imagery is included.
- **No raster assets** ship with this bundle.

## Files in this bundle
- `Notice.html` — prototype entry/wiring.
- `app/data.jsx` — taxonomy, color helpers, mock data (**read first**).
- `app/ui.jsx`, `app/feed.jsx`, `app/panels.jsx`, `app/onboarding.jsx`, `app/journey.jsx`, `app/main.jsx` — screen logic.
- `app/styles.css` — **all visual specs / tokens** (502 lines; the authoritative source for exact values).
- `ios-frame.jsx`, `tweaks-panel.jsx`, `image-slot.js` — prototype scaffolding only; **do not port**.

To preview the reference: open `Notice.html` in a browser. Use the in-prototype **Tweaks → Demo state** (anon / new / established) and **Replay first-time moment** to see every state, including the first-time journey.
