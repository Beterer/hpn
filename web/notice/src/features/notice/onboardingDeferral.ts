// "Browse for now" lets a signed-in member step out of profile setup into the feed
// shell. The choice is persisted per account so a refresh doesn't re-trap them, while a
// *different* member signing in on the same browser still gets their own first-run
// onboarding. localStorage access is guarded the same way as notice.toastedNotificationIds
// so private mode / quota errors degrade to "not deferred" rather than throwing.
const KEY_PREFIX = 'notice.onboardingDeferred.'

export function readDeferred(accountId: string | null): boolean {
  if (!accountId) return false
  try {
    return localStorage.getItem(KEY_PREFIX + accountId) === '1'
  } catch {
    return false
  }
}

export function writeDeferred(accountId: string | null): void {
  if (!accountId) return
  try {
    localStorage.setItem(KEY_PREFIX + accountId, '1')
  } catch {
    // localStorage unavailable — deferral degrades to per-session (state still holds).
  }
}

export function clearDeferred(accountId: string | null): void {
  if (!accountId) return
  try {
    localStorage.removeItem(KEY_PREFIX + accountId)
  } catch {
    // ignore
  }
}
