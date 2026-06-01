import type { Profile } from '../../lib/api/profile'
import { useMyPhotos } from '../../lib/query/photos'
import { useUpdateProfileStatus } from '../../lib/query/profile'
import { InterestPicker } from './InterestPicker'
import { PhotoManager } from './PhotoManager'
import { ProfileForm } from './ProfileForm'

function statusLabel(status: string) {
  if (status === 'active') return 'Active'
  if (status === 'paused') return 'Paused'
  return 'Draft'
}

export function ProfileEditor({ profile }: { profile: Profile }) {
  const statusMutation = useUpdateProfileStatus()
  const photos = useMyPhotos()
  const nextStatus = profile.status === 'active' ? 'paused' : 'active'
  const canActivate = profile.status === 'active' || (photos.data?.length ?? 0) > 0

  return (
    <main className="mx-auto grid w-full max-w-5xl gap-6 px-6 py-10">
      <section className="grid gap-4 border-b border-zinc-200 pb-6 sm:grid-cols-[1fr_auto] sm:items-end">
        <div>
          <p className="text-sm font-semibold uppercase tracking-widest text-teal-700">Profile</p>
          <h1 className="mt-2 text-3xl font-semibold text-zinc-950">{profile.displayName}</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-zinc-600">
            Keep the profile grounded in what people can appreciate. Notice is not a dating app, and this
            surface stays away from scores and comparison.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <span
            className={`rounded-lg px-3 py-1 text-sm font-medium ${
              profile.status === 'active'
                ? 'bg-emerald-50 text-emerald-900'
                : 'bg-amber-50 text-amber-900'
            }`}
          >
            {statusLabel(profile.status)}
            {profile.verified ? ' / Verified' : ''}
          </span>
          <button
            type="button"
            disabled={statusMutation.isPending || !canActivate}
            onClick={() => statusMutation.mutate(nextStatus)}
            className="rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 transition hover:border-teal-700 hover:text-teal-800 disabled:opacity-60"
          >
            {statusMutation.isPending
              ? 'Updating…'
              : profile.status === 'active'
                ? 'Pause profile'
                : 'Activate profile'}
          </button>
        </div>
      </section>
      {statusMutation.isError && <p className="text-sm text-rose-700">{statusMutation.error.message}</p>}

      <section className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="grid gap-6">
          <div className="rounded-lg border border-zinc-200 bg-white p-5 shadow-sm">
            <PhotoManager compact />
          </div>

          <div className="rounded-lg border border-zinc-200 bg-white p-5 shadow-sm">
            <h2 className="mb-5 text-xl font-semibold text-zinc-950">Basics</h2>
            <ProfileForm profile={profile} />
          </div>
        </div>

        <div className="rounded-lg border border-zinc-200 bg-white p-5 shadow-sm">
          <h2 className="mb-5 text-xl font-semibold text-zinc-950">Interests</h2>
          <InterestPicker profile={profile} />
        </div>
      </section>
    </main>
  )
}
