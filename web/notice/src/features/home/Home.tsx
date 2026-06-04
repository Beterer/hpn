import { useMe } from '../../lib/query/auth'
import { NoticeApp } from '../notice/NoticeApp'

/**
 * The root screen. /me decides account state (the cookie is httpOnly, so the
 * server is the only source of truth — backbone §9.2). NoticeApp renders the
 * anon or member experience; signed-out visitors browse the feed as guests.
 */
export function Home() {
  const { data: me, isLoading } = useMe()

  if (isLoading) {
    return (
      <div className="notice-root">
        <div className="app-root">
          <div className="centered-note">Opening Notice…</div>
        </div>
      </div>
    )
  }

  return <NoticeApp me={me ?? null} />
}
