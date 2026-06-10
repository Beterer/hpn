import { NavIcon, Wordmark, type NavName } from './ui'

export function AppHeader({
  anon,
  incomplete = false,
  onNudge,
  onResume,
  onGear,
}: {
  anon: boolean
  incomplete?: boolean
  onNudge: () => void
  onResume?: () => void
  onGear: () => void
}) {
  return (
    <header className="app-header">
      <Wordmark />
      {anon ? (
        <button className="nudge" onClick={onNudge} aria-label="Create a profile to be noticed back">
          <span className="nudge-halo" />
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="9" cy="8" r="3.4" />
            <path d="M3.5 20a5.5 5.5 0 0 1 11 0" />
            <path d="M18 7v6M21 10h-6" />
          </svg>
          <span className="nudge-label">Be noticed back</span>
        </button>
      ) : incomplete ? (
        <button className="nudge" onClick={onResume} aria-label="Finish setting up your profile">
          <span className="nudge-halo" />
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="9" cy="8" r="3.4" />
            <path d="M3.5 20a5.5 5.5 0 0 1 11 0" />
            <path d="M18 7v6M21 10h-6" />
          </svg>
          <span className="nudge-label">Finish your profile</span>
        </button>
      ) : (
        <button className="ghost-btn" onClick={onGear} aria-label="Settings">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="3" />
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
          </svg>
        </button>
      )}
    </header>
  )
}

const TABS: { id: NavName; label: string }[] = [
  { id: 'feed', label: 'Notice' },
  { id: 'received', label: 'Received' },
  { id: 'fingerprint', label: 'Fingerprint' },
  { id: 'you', label: 'You' },
]

export function BottomNav({
  tab,
  onTab,
  hasUnseen = false,
}: {
  tab: NavName
  onTab: (t: NavName) => void
  hasUnseen?: boolean
}) {
  return (
    <nav className="bottom-nav">
      {TABS.map((t) => (
        <button
          key={t.id}
          className={`nav-item ${tab === t.id ? 'active' : ''}`}
          onClick={() => onTab(t.id)}
          aria-current={tab === t.id ? 'page' : undefined}
        >
          <span className="nav-icon-wrap">
            <NavIcon name={t.id} active={tab === t.id} />
            {t.id === 'received' && hasUnseen && <span className="nav-dot" aria-label="New appreciation" />}
          </span>
          <span>{t.label}</span>
        </button>
      ))}
    </nav>
  )
}
