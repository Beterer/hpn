import { useState } from 'react'
import type { FeedProfile } from '../../lib/api/feed'
import { REPORT_TYPES } from '../../lib/api/reports'
import { useSubmitReport } from '../../lib/query/reports'

const GENDER_LABELS: Record<string, string> = {
  woman: 'Woman',
  man: 'Man',
  non_binary: 'Non-binary',
  self_describe: 'Self-described',
}

function genderLabel(profile: FeedProfile): string {
  if (profile.gender === 'self_describe' && profile.selfDescribeText) {
    return profile.selfDescribeText
  }
  return GENDER_LABELS[profile.gender ?? ''] ?? ''
}

// Coarse distance bands only — the API never sends an exact distance (§10.4).
const DISTANCE_LABELS: Record<string, string> = {
  nearby: 'Nearby',
  under_50km: 'Under 50 km',
  '50_200km': '50–200 km',
  '200km_plus': '200+ km',
  different_country: 'Different country',
}

function distanceLabel(bucket: string | null | undefined): string {
  return bucket ? (DISTANCE_LABELS[bucket] ?? '') : ''
}

/**
 * One feed card (backbone §6.5, §9.4). A calm, single-profile surface — no
 * counts, no scores, and deliberately no skip or dislike affordance. Advancing
 * the feed requires choosing an appreciation.
 */
export function FeedCard({ profile }: { profile: FeedProfile }) {
  const photos = profile.photos ?? []
  const [active, setActive] = useState(0)
  const photo = photos[Math.min(active, Math.max(photos.length - 1, 0))]

  return (
    <article className="overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-sm">
      <div className="relative aspect-[4/5] w-full bg-stone-100">
        {photo ? (
          <img
            src={photo.displayUrl ?? ''}
            alt={`A photo ${profile.displayName ?? ''} shared`}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-zinc-400">No photo</div>
        )}

        {photos.length > 1 && (
          <div className="absolute inset-x-0 bottom-0 flex justify-center gap-1.5 p-3">
            {photos.map((p, index) => (
              <button
                key={p.photoId}
                type="button"
                aria-label={`Show photo ${index + 1}`}
                onClick={() => setActive(index)}
                className={`h-1.5 rounded-full transition-all ${
                  index === active ? 'w-6 bg-white' : 'w-3 bg-white/60'
                }`}
              />
            ))}
          </div>
        )}
      </div>

      <div className="grid gap-2 p-5">
        <div className="flex flex-wrap items-baseline gap-x-3">
          <h2 className="text-2xl font-semibold text-zinc-950">{profile.displayName}</h2>
          {profile.verified && (
            <span className="rounded-md bg-teal-50 px-2 py-0.5 text-xs font-medium text-teal-800">Verified</span>
          )}
        </div>
        <p className="text-sm text-zinc-500">
          {[genderLabel(profile), profile.countryCode, distanceLabel(profile.distanceBucket)]
            .filter(Boolean)
            .join(' · ')}
        </p>
        {profile.bio && <p className="mt-1 text-sm leading-6 text-zinc-700">{profile.bio}</p>}

        {profile.profileId && <ReportControl profileId={profile.profileId} />}
      </div>
    </article>
  )
}

/**
 * A quiet, always-reachable way to report a profile (backbone §6.7, §11). Kept
 * understated so the surface stays appreciation-first (§2); intake is acknowledged,
 * never scored back to the reporter.
 */
function ReportControl({ profileId }: { profileId: string }) {
  const [open, setOpen] = useState(false)
  const report = useSubmitReport()

  if (report.isSuccess) {
    return (
      <p className="mt-2 text-xs text-zinc-500">Thanks — our team will take a look.</p>
    )
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="mt-2 self-start text-xs font-medium text-zinc-400 hover:text-zinc-600"
      >
        Report this profile
      </button>
    )
  }

  return (
    <div className="mt-2 rounded-lg border border-zinc-200 bg-stone-50 p-3">
      <p className="text-xs font-medium text-zinc-700">Why are you reporting this profile?</p>
      <div className="mt-2 flex flex-wrap gap-1.5">
        {REPORT_TYPES.map((t) => (
          <button
            key={t.value}
            type="button"
            disabled={report.isPending}
            onClick={() => report.mutate({ targetProfileId: profileId, type: t.value })}
            className="rounded-full border border-zinc-300 px-2.5 py-1 text-xs text-zinc-700 hover:border-rose-400 hover:text-rose-700 disabled:opacity-60"
          >
            {t.label}
          </button>
        ))}
      </div>
      {report.isError && <p className="mt-2 text-xs text-rose-700">{report.error.message}</p>}
      <button
        type="button"
        onClick={() => setOpen(false)}
        className="mt-2 text-xs font-medium text-zinc-500 hover:text-zinc-800"
      >
        Cancel
      </button>
    </div>
  )
}
