import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { requestMagicLink } from '../../lib/api/auth'
import { Wordmark } from './ui'

/**
 * Auth entry that doubles as sign-in (ADR-025 redesign). Opens over the feed for
 * anonymous browsers via the "Be noticed back" nudge or the You tab. Email magic
 * link only — the SPA never handles tokens (ADR-012). Same 202 response whether
 * or not the account exists, so copy never reveals existence.
 */
export function AuthFlow({ onClose }: { onClose: () => void }) {
  const [mode, setMode] = useState<'create' | 'signin'>('create')
  const [email, setEmail] = useState('')
  const magic = useMutation({ mutationFn: (value: string) => requestMagicLink(value) })

  const valid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
  const create = mode === 'create'

  return (
    <div className="ob welcome">
      <div className="ob-brand">
        <Wordmark />
        <button className="ghost-btn" onClick={onClose} aria-label="Keep browsing">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
        </button>
      </div>

      <div className="welcome-mid">
        {magic.isSuccess ? (
          <>
            <h1 className="welcome-h">Check your <em>inbox</em>.</h1>
            <p className="welcome-sub">
              If {email} can sign in, a one-time link is on its way. It expires in about 15 minutes.
            </p>
          </>
        ) : (
          <>
            <h1 className="welcome-h">{create ? <>Be <em>noticed</em> back.</> : <>Welcome <em>back</em>.</>}</h1>
            <p className="welcome-sub">
              {create
                ? 'Set up a profile so the people you appreciate can appreciate you too. No passwords — just your email.'
                : 'Sign in with your email. We send a one-time link; no passwords to remember.'}
            </p>

            <label className="field" style={{ marginTop: 22 }}>
              Email
              <input
                type="email"
                autoComplete="email"
                inputMode="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </label>
            {magic.isError && <p className="ob-error">Something went wrong. Please try again.</p>}
          </>
        )}
      </div>

      {!magic.isSuccess && (
        <div className="welcome-actions">
          <button
            className="big-btn primary"
            disabled={!valid || magic.isPending}
            onClick={() => magic.mutate(email)}
          >
            {magic.isPending ? 'Sending…' : create ? 'Create my profile' : 'Sign in'}
          </button>
          <button className="big-btn ghost" onClick={() => setMode(create ? 'signin' : 'create')}>
            {create ? 'I already have an account' : 'New here? Create a profile'}
          </button>
          <button className="welcome-skip" onClick={onClose}>Keep browsing anonymously</button>
        </div>
      )}
    </div>
  )
}
