import { useState } from 'react'
import type { FeedProfile } from '../../lib/api/feed'

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

/**
 * One feed card (backbone §6.5, §9.4). A calm, single-profile surface — no
 * counts, no scores, and deliberately no skip or dislike affordance. Advancing
 * the feed will require choosing an appreciation; that chooser arrives in M5.
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
          {[genderLabel(profile), profile.countryCode].filter(Boolean).join(' · ')}
        </p>
        {profile.bio && <p className="mt-1 text-sm leading-6 text-zinc-700">{profile.bio}</p>}
      </div>
    </article>
  )
}
