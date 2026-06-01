import type { Me } from '../../lib/api/auth'
import type { Profile } from '../../lib/api/profile'
import { useMyPhotos } from '../../lib/query/photos'
import { useUpdateProfileStatus } from '../../lib/query/profile'
import { InterestPicker } from './InterestPicker'
import { PhotoManager } from './PhotoManager'
import { ProfileForm } from './ProfileForm'

type Step = 'gender' | 'interests' | 'photos' | 'visibility'

const steps = [
  { id: 'account', label: 'Account' },
  { id: 'gender', label: 'Gender' },
  { id: 'interests', label: 'Interests' },
  { id: 'photos', label: 'Photos' },
  { id: 'visibility', label: 'Visibility' },
]

export function OnboardingFlow({ me, profile }: { me: Me; profile: Profile | null }) {
  const statusMutation = useUpdateProfileStatus()
  const photos = useMyPhotos(Boolean(profile))
  const photoCount = photos.data?.length ?? 0
  const step: Step = !profile
    ? 'gender'
    : profile.interests.length === 0
      ? 'interests'
      : photos.isLoading || photoCount === 0
        ? 'photos'
        : 'visibility'

  const activeIndex = steps.findIndex((item) => item.id === step)

  return (
    <div className="mx-auto grid w-full max-w-5xl gap-8 px-6 py-10 lg:grid-cols-[280px_1fr]">
      <aside className="grid content-start gap-5">
        <div>
          <p className="text-sm font-semibold uppercase tracking-widest text-teal-700">Notice</p>
          <h1 className="mt-3 text-3xl font-semibold text-zinc-950">Set up how you want to be noticed.</h1>
        </div>
        <p className="text-sm leading-6 text-zinc-600">
          Notice is not a dating app. This profile leaves out age, height, body type, income, scores, and
          public counts.
        </p>
        <ol className="grid gap-2">
          {steps.map((item, index) => {
            const isComplete = item.id === 'account' || index < activeIndex
            const isCurrent = item.id === step
            return (
              <li
                key={item.id}
                className={`flex items-center gap-3 rounded-lg border px-3 py-2 text-sm ${
                  isCurrent
                    ? 'border-teal-700 bg-teal-50 text-teal-950'
                    : isComplete
                      ? 'border-emerald-200 bg-emerald-50 text-emerald-950'
                      : 'border-zinc-200 bg-white text-zinc-500'
                }`}
              >
                <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-white text-xs font-semibold">
                  {index + 1}
                </span>
                {item.label}
              </li>
            )
          })}
        </ol>
      </aside>

      <section className="rounded-lg border border-zinc-200 bg-white p-5 shadow-sm">
        {step === 'gender' && (
          <div className="grid gap-5">
            <div>
              <p className="text-sm text-zinc-500">{me.user.email}</p>
              <h2 className="mt-1 text-2xl font-semibold text-zinc-950">Profile basics</h2>
            </div>
            <ProfileForm profile={profile} submitLabel="Continue" />
          </div>
        )}

        {step === 'interests' && (
          <div className="grid gap-5">
            <div>
              <h2 className="text-2xl font-semibold text-zinc-950">Interests</h2>
              <p className="mt-1 text-sm text-zinc-600">
                Pick a few ordinary places where appreciation might begin.
              </p>
            </div>
            <InterestPicker profile={profile} submitLabel="Continue" />
          </div>
        )}

        {step === 'visibility' && (
          <div className="grid gap-5">
            <div>
              <h2 className="text-2xl font-semibold text-zinc-950">Visibility</h2>
              <p className="mt-1 text-sm text-zinc-600">
                Your first profile starts with broad, low-risk defaults. Privacy controls get a fuller pass in
                settings later.
              </p>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="rounded-lg border border-zinc-200 p-3">
                <p className="text-sm font-medium text-zinc-950">Outside-country only</p>
                <p className="mt-1 text-sm text-zinc-500">Off</p>
              </div>
              <div className="rounded-lg border border-zinc-200 p-3">
                <p className="text-sm font-medium text-zinc-950">Women-for-women</p>
                <p className="mt-1 text-sm text-zinc-500">Off</p>
              </div>
              <div className="rounded-lg border border-zinc-200 p-3">
                <p className="text-sm font-medium text-zinc-950">Verified only</p>
                <p className="mt-1 text-sm text-zinc-500">Off</p>
              </div>
              <div className="rounded-lg border border-zinc-200 p-3">
                <p className="text-sm font-medium text-zinc-950">Precise location</p>
                <p className="mt-1 text-sm text-zinc-500">Not collected</p>
              </div>
            </div>
            {statusMutation.isError && (
              <p className="text-sm text-rose-700">
                {statusMutation.error.message || 'Your profile could not be activated.'}
              </p>
            )}
            <div>
              <button
                type="button"
                disabled={!profile || photoCount === 0 || statusMutation.isPending}
                onClick={() => statusMutation.mutate('active')}
                className="rounded-lg bg-zinc-950 px-4 py-2 font-medium text-white transition hover:bg-teal-800 disabled:opacity-60"
              >
                {statusMutation.isPending ? 'Activating…' : 'Activate profile'}
              </button>
            </div>
          </div>
        )}

        {step === 'photos' && <PhotoManager enabled={Boolean(profile)} />}
      </section>
    </div>
  )
}
