import { MagicLinkForm } from '../auth/MagicLinkForm'

export function LandingPage() {
  return (
    <main className="mx-auto flex min-h-full max-w-2xl flex-col justify-center gap-6 px-6 py-16">
      <p className="text-sm font-medium uppercase tracking-widest text-slate-400">
        Notice
      </p>
      <h1 className="text-4xl font-semibold text-slate-900">
        Appreciation reveals as much about you as the people you notice.
      </h1>
      <p className="text-lg leading-relaxed text-slate-600">
        Notice is a positive-only space: you move forward by appreciating a
        specific quality in someone — never by judging, rating, or swiping away.
        Over time you build a sense of how others perceive you, and of what you
        tend to notice in them.
      </p>
      <p className="text-base text-slate-500">
        It is not a dating app. There are no scores, rankings, or public counts.
      </p>

      <div className="mt-2 max-w-sm">
        <MagicLinkForm />
      </div>
    </main>
  )
}
