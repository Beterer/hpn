import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { verifyMagicLink } from '../../lib/api/auth'
import { authKeys } from '../../lib/query/auth'

type State = 'verifying' | 'error'

/**
 * Landing route for the emailed link (backbone §9.3). Posts the token to
 * /auth/verify, which sets the session cookie, then refreshes /me and routes
 * into the app. The SPA never stores the token (§10.1).
 */
export function VerifyPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [state, setState] = useState<State>(searchParams.get('token') ? 'verifying' : 'error')
  const attempted = useRef(false)

  const token = searchParams.get('token')

  useEffect(() => {
    if (attempted.current || !token) {
      return
    }
    attempted.current = true

    verifyMagicLink(token)
      .then(async () => {
        await queryClient.invalidateQueries({ queryKey: authKeys.me })
        navigate('/', { replace: true })
      })
      .catch(() => setState('error'))
  }, [token, navigate, queryClient])

  return (
    <main className="mx-auto flex min-h-full max-w-md flex-col justify-center gap-4 px-6 py-16 text-center">
      {state === 'verifying' ? (
        <p className="text-slate-600">Signing you in…</p>
      ) : (
        <>
          <h1 className="text-2xl font-semibold text-slate-900">That link didn’t work</h1>
          <p className="text-slate-600">
            It may have expired or already been used. Head back and request a fresh one.
          </p>
          <button
            type="button"
            onClick={() => navigate('/', { replace: true })}
            className="mx-auto rounded-lg bg-slate-900 px-4 py-2 font-medium text-white hover:bg-slate-700"
          >
            Back to sign in
          </button>
        </>
      )}
    </main>
  )
}
