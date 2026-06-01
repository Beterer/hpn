import { useMutation } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { requestMagicLink } from '../../lib/api/auth'

type FormValues = { email: string }

/**
 * Email-only sign-in (no passwords — backbone §11). On submit we show the same
 * "check your inbox" confirmation whether or not the address has an account, so
 * the UI never reveals existence either (§10.1).
 */
export function MagicLinkForm() {
  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ defaultValues: { email: '' } })

  const mutation = useMutation({
    mutationFn: (email: string) => requestMagicLink(email),
  })

  if (mutation.isSuccess) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-6 text-slate-700">
        <p className="font-medium text-slate-900">Check your inbox</p>
        <p className="mt-1 text-sm">
          If {getValues('email')} can sign in, a one-time link is on its way. It expires in about 15
          minutes.
        </p>
      </div>
    )
  }

  return (
    <form
      onSubmit={handleSubmit((values) => mutation.mutate(values.email))}
      className="flex flex-col gap-3"
      noValidate
    >
      <label htmlFor="email" className="text-sm font-medium text-slate-700">
        Email
      </label>
      <input
        id="email"
        type="email"
        autoComplete="email"
        placeholder="you@example.com"
        className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-900"
        {...register('email', {
          required: 'Enter your email to continue.',
          pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'That email doesn’t look right.' },
        })}
      />
      {errors.email && <p className="text-sm text-red-600">{errors.email.message}</p>}
      {mutation.isError && (
        <p className="text-sm text-red-600">Something went wrong. Please try again.</p>
      )}
      <button
        type="submit"
        disabled={isSubmitting || mutation.isPending}
        className="rounded-lg bg-slate-900 px-4 py-2 font-medium text-white transition hover:bg-slate-700 disabled:opacity-60"
      >
        {mutation.isPending ? 'Sending…' : 'Email me a sign-in link'}
      </button>
    </form>
  )
}
