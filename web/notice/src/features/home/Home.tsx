import { useMe } from '../../lib/query/auth'
import { LandingPage } from '../landing/LandingPage'
import { AppShell } from '../shell/AppShell'

/**
 * The root screen: landing for signed-out visitors, the app shell once a session
 * is established (backbone §9.3). /me decides which — the cookie is httpOnly, so
 * the server is the only source of truth about auth state (§9.2).
 */
export function Home() {
  const { data: me, isLoading } = useMe()

  if (isLoading) {
    return (
      <main className="flex min-h-full items-center justify-center">
        <p className="text-slate-500">Loading…</p>
      </main>
    )
  }

  return me ? <AppShell me={me} /> : <LandingPage />
}
