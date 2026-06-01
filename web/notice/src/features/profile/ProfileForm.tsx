import { useEffect } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import type { Profile, UpsertProfileInput } from '../../lib/api/profile'
import { useUpsertProfile } from '../../lib/query/profile'

type FormValues = {
  displayName: string
  gender: string
  selfDescribeText: string
  countryCode: string
  bio: string
}

const genderOptions = [
  { value: 'woman', label: 'Woman' },
  { value: 'man', label: 'Man' },
  { value: 'non_binary', label: 'Non-binary' },
  { value: 'self_describe', label: 'Self-describe' },
]

function defaults(profile: Profile | null): FormValues {
  return {
    displayName: profile?.displayName ?? '',
    gender: profile?.gender ?? 'woman',
    selfDescribeText: profile?.selfDescribeText ?? '',
    countryCode: profile?.countryCode ?? '',
    bio: profile?.bio ?? '',
  }
}

function toInput(values: FormValues): UpsertProfileInput {
  const gender = values.gender
  return {
    displayName: values.displayName.trim(),
    gender,
    selfDescribeText:
      gender === 'self_describe' && values.selfDescribeText.trim()
        ? values.selfDescribeText.trim()
        : null,
    countryCode: values.countryCode.trim() ? values.countryCode.trim().toUpperCase() : null,
    bio: values.bio.trim() ? values.bio.trim() : null,
  }
}

export function ProfileForm({
  profile,
  onSaved,
  submitLabel = 'Save profile',
}: {
  profile: Profile | null
  onSaved?: (profile: Profile) => void
  submitLabel?: string
}) {
  const mutation = useUpsertProfile()
  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ defaultValues: defaults(profile), shouldUnregister: true })

  useEffect(() => {
    reset(defaults(profile))
  }, [profile, reset])

  const gender = useWatch({ control, name: 'gender' })

  return (
    <form
      className="grid gap-4"
      noValidate
      onSubmit={handleSubmit((values) =>
        mutation.mutate(toInput(values), {
          onSuccess: (saved) => onSaved?.(saved),
        }),
      )}
    >
      <label className="grid gap-2 text-sm font-medium text-zinc-700">
        Display name
        <input
          className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-base text-zinc-950 outline-none focus:border-teal-700"
          autoComplete="nickname"
          {...register('displayName', {
            required: 'Add a display name.',
            minLength: { value: 2, message: 'Use at least 2 characters.' },
            maxLength: { value: 80, message: 'Use 80 characters or fewer.' },
          })}
        />
        {errors.displayName && <span className="text-sm text-rose-700">{errors.displayName.message}</span>}
      </label>

      <fieldset className="grid gap-2">
        <legend className="text-sm font-medium text-zinc-700">Gender</legend>
        <div className="grid gap-2 sm:grid-cols-2">
          {genderOptions.map((option) => (
            <label
              key={option.value}
              className="flex cursor-pointer items-center gap-2 rounded-lg border border-zinc-300 px-3 py-2 text-sm text-zinc-800 has-[:checked]:border-teal-700 has-[:checked]:bg-teal-50"
            >
              <input
                type="radio"
                value={option.value}
                className="size-4 accent-teal-700"
                {...register('gender')}
              />
              {option.label}
            </label>
          ))}
        </div>
      </fieldset>

      {gender === 'self_describe' && (
        <label className="grid gap-2 text-sm font-medium text-zinc-700">
          Self-describe
          <input
            className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-base text-zinc-950 outline-none focus:border-teal-700"
            {...register('selfDescribeText', {
              required: 'Add the words you want shown.',
              maxLength: { value: 80, message: 'Use 80 characters or fewer.' },
            })}
          />
          {errors.selfDescribeText && (
            <span className="text-sm text-rose-700">{errors.selfDescribeText.message}</span>
          )}
        </label>
      )}

      <label className="grid gap-2 text-sm font-medium text-zinc-700">
        Country
        <input
          className="w-28 rounded-lg border border-zinc-300 bg-white px-3 py-2 text-base uppercase text-zinc-950 outline-none focus:border-teal-700"
          placeholder="RO"
          maxLength={2}
          autoComplete="country"
          {...register('countryCode', {
            pattern: { value: /^[A-Za-z]{2}$/, message: 'Use a 2-letter country code.' },
          })}
        />
        {errors.countryCode && <span className="text-sm text-rose-700">{errors.countryCode.message}</span>}
      </label>

      <label className="grid gap-2 text-sm font-medium text-zinc-700">
        Bio
        <textarea
          className="min-h-28 resize-y rounded-lg border border-zinc-300 bg-white px-3 py-2 text-base text-zinc-950 outline-none focus:border-teal-700"
          maxLength={500}
          {...register('bio', {
            maxLength: { value: 500, message: 'Use 500 characters or fewer.' },
          })}
        />
        {errors.bio && <span className="text-sm text-rose-700">{errors.bio.message}</span>}
      </label>

      {mutation.isError && <p className="text-sm text-rose-700">The profile could not be saved.</p>}

      <div>
        <button
          type="submit"
          disabled={isSubmitting || mutation.isPending}
          className="rounded-lg bg-zinc-950 px-4 py-2 font-medium text-white transition hover:bg-teal-800 disabled:opacity-60"
        >
          {mutation.isPending ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
