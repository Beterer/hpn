import { useState } from 'react'
import type { Profile } from '../../lib/api/profile'
import { useInterests, useUpdateProfileInterests } from '../../lib/query/profile'

export function InterestPicker({
  profile,
  onSaved,
  submitLabel = 'Save interests',
}: {
  profile: Profile | null
  onSaved?: (profile: Profile) => void
  submitLabel?: string
}) {
  const selectedKey = profile?.interests.map((interest) => interest.id).sort().join(':') ?? 'none'
  return (
    <InterestPickerInner
      key={`${profile?.id ?? 'none'}:${selectedKey}`}
      profile={profile}
      onSaved={onSaved}
      submitLabel={submitLabel}
    />
  )
}

function InterestPickerInner({
  profile,
  onSaved,
  submitLabel,
}: {
  profile: Profile | null
  onSaved?: (profile: Profile) => void
  submitLabel: string
}) {
  const interests = useInterests()
  const mutation = useUpdateProfileInterests()
  const [selected, setSelected] = useState<string[]>(() => profile?.interests.map((interest) => interest.id) ?? [])

  function toggle(id: string) {
    setSelected((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
    )
  }

  if (interests.isLoading) {
    return <p className="text-zinc-500">Loading interests…</p>
  }

  if (interests.isError) {
    return <p className="text-rose-700">Interests could not be loaded.</p>
  }

  return (
    <div className="grid gap-4">
      <div className="grid gap-2 sm:grid-cols-3">
        {interests.data?.map((interest) => {
          const active = selected.includes(interest.id)
          return (
            <button
              key={interest.id}
              type="button"
              onClick={() => toggle(interest.id)}
              className={`rounded-lg border px-3 py-2 text-left text-sm font-medium transition ${
                active
                  ? 'border-teal-700 bg-teal-50 text-teal-950'
                  : 'border-zinc-300 bg-white text-zinc-700 hover:border-amber-500'
              }`}
            >
              {interest.label}
            </button>
          )
        })}
      </div>

      {mutation.isError && <p className="text-sm text-rose-700">The interests could not be saved.</p>}

      <div>
        <button
          type="button"
          disabled={!profile || mutation.isPending}
          onClick={() =>
            mutation.mutate(selected, {
              onSuccess: (saved) => onSaved?.(saved),
            })
          }
          className="rounded-lg bg-zinc-950 px-4 py-2 font-medium text-white transition hover:bg-teal-800 disabled:opacity-60"
        >
          {mutation.isPending ? 'Saving…' : submitLabel}
        </button>
      </div>
    </div>
  )
}
