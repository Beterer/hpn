import { useEffect } from 'react'
import type { NotificationItem } from '../../lib/api/notifications'
import { cat } from './colors'
import { CATEGORY_HUE } from './taxonomy'

export function IncomingToast({
  item,
  onOpen,
  onDismiss,
}: {
  item: NotificationItem
  onOpen: () => void
  onDismiss: () => void
}) {
  useEffect(() => {
    const id = setTimeout(onDismiss, 8000)
    return () => clearTimeout(id)
  }, [onDismiss])

  const hue = CATEGORY_HUE[item.categorySlug] ?? 38
  const title = `Someone just noticed your ${item.traitLabel.toLowerCase()}.`

  return (
    <button className="toast" type="button" onClick={onOpen}>
      <span className="toast-burst" style={{ background: cat(hue) }} />
      <span className="toast-dot" style={{ background: cat(hue) }}>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
          <path d="M8 12.5l3 3 5-6" />
        </svg>
      </span>
      <span className="toast-text">
        <span className="toast-title">{title}</span>
        <span className="toast-sub">Tap to see what they appreciated</span>
      </span>
      <span
        className="toast-x"
        onClick={(event) => {
          event.stopPropagation()
          onDismiss()
        }}
        aria-label="Dismiss"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M6 6l12 12M18 6 6 18" />
        </svg>
      </span>
    </button>
  )
}
