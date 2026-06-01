import { useRef, useState } from 'react'
import type { FeedProfile } from '../../lib/api/feed'
import { useAppreciationCategories, useSubmitAppreciation } from '../../lib/query/appreciation'

interface AppreciationChooserProps {
  profile: FeedProfile
  onUnlocked: () => void
}

export function AppreciationChooser({ profile, onUnlocked }: AppreciationChooserProps) {
  const categories = useAppreciationCategories()
  const submit = useSubmitAppreciation()
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)
  // One idempotency key per category choice on this card (the chooser is keyed by
  // profile id, so it resets per card). Reusing the key on a retry lets the server
  // replay the original outcome instead of treating the retry as a new request.
  const idempotencyKeys = useRef<Map<string, string>>(new Map())
  const primaryPhotoId =
    profile.photos?.find((photo) => Number(photo.position) === 0)?.photoId ?? profile.photos?.[0]?.photoId ?? null

  function choose(categoryId: string) {
    if (!profile.profileId || submit.isPending) {
      return
    }

    let idempotencyKey = idempotencyKeys.current.get(categoryId)
    if (!idempotencyKey) {
      idempotencyKey = crypto.randomUUID()
      idempotencyKeys.current.set(categoryId, idempotencyKey)
    }

    setSelectedCategoryId(categoryId)
    submit.mutate(
      {
        request: {
          receiverProfileId: profile.profileId,
          categoryId,
          photoId: primaryPhotoId,
        },
        idempotencyKey,
      },
      {
        onSuccess: onUnlocked,
      },
    )
  }

  if (categories.isLoading) {
    return <div className="h-28 rounded-lg border border-zinc-200 bg-white" />
  }

  if (categories.isError) {
    return (
      <div className="rounded-lg border border-rose-200 bg-rose-50 p-3 text-center text-sm text-rose-700">
        {(categories.error as Error).message}
      </div>
    )
  }

  return (
    <section className="grid gap-3">
      <h3 className="text-center text-sm font-semibold uppercase tracking-widest text-teal-800">
        What did you notice?
      </h3>
      <div className="grid grid-cols-2 gap-2">
        {categories.data?.map((category) => {
          const active = selectedCategoryId === category.id && submit.isPending
          return (
            <button
              key={category.id}
              type="button"
              onClick={() => void choose(category.id)}
              disabled={submit.isPending}
              className={`min-h-11 rounded-lg border px-3 py-2 text-sm font-medium transition ${
                active
                  ? 'border-teal-700 bg-teal-700 text-white'
                  : 'border-zinc-300 bg-white text-zinc-800 hover:border-teal-700 hover:text-teal-800'
              } disabled:cursor-not-allowed disabled:opacity-70`}
            >
              {category.label}
            </button>
          )
        })}
      </div>
      {submit.isError && (
        <p className="text-center text-sm text-rose-700">{submit.error.message}</p>
      )}
    </section>
  )
}
