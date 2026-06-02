import { useState } from 'react'
import type { Profile } from '../../lib/api/profile'
import {
  useBlocks,
  useExportAccount,
  useRequestAccountDeletion,
  useUnblock,
  useUpdateLocation,
  useUpdateVisibility,
} from '../../lib/query/settings'

type VisibilityForm = {
  showOnlyOutsideCountry: boolean
  hideFromCountry: boolean
  minDistanceKm: number | null
  womenForWomen: boolean
  verifiedOnly: boolean
  paused: boolean
}

/**
 * Privacy & settings (backbone §8 Settings, §10.4, §10.5). Every trust control —
 * audience toggles, coarse location, blocks, data export and account deletion —
 * is reachable here. Copy stays plain and non-competitive (§2).
 */
export function SettingsView({ profile }: { profile: Profile }) {
  const prefs = profile.visibilityPreferences
  const [form, setForm] = useState<VisibilityForm>({
    showOnlyOutsideCountry: prefs.showOnlyOutsideCountry,
    hideFromCountry: prefs.hideFromCountry,
    minDistanceKm: prefs.minDistanceKm == null ? null : Number(prefs.minDistanceKm),
    womenForWomen: prefs.womenForWomen,
    verifiedOnly: prefs.verifiedOnly,
    paused: prefs.paused,
  })

  const visibility = useUpdateVisibility()
  const location = useUpdateLocation()
  const blocks = useBlocks()
  const unblock = useUnblock()
  const exportData = useExportAccount()
  const deletion = useRequestAccountDeletion()

  const [confirmDelete, setConfirmDelete] = useState(false)
  const [locationMessage, setLocationMessage] = useState<string | null>(null)

  const saveVisibility = () => {
    visibility.mutate({
      showOnlyOutsideCountry: form.showOnlyOutsideCountry,
      hideFromCountry: form.hideFromCountry,
      minDistanceKm: form.minDistanceKm,
      womenForWomen: form.womenForWomen,
      verifiedOnly: form.verifiedOnly,
      paused: form.paused,
    })
  }

  const shareLocation = () => {
    setLocationMessage(null)
    if (!('geolocation' in navigator)) {
      setLocationMessage('Your browser cannot share a location.')
      return
    }
    navigator.geolocation.getCurrentPosition(
      (position) => {
        location.mutate(
          {
            consent: true,
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          },
          {
            onSuccess: () => setLocationMessage('Location saved (kept coarse, ~11 km).'),
            onError: (error) => setLocationMessage(error.message),
          },
        )
      },
      () => setLocationMessage('We could not read your location.'),
    )
  }

  const stopSharingLocation = () => {
    setLocationMessage(null)
    location.mutate(
      { consent: false, latitude: null, longitude: null },
      {
        onSuccess: () => setLocationMessage('Location sharing turned off.'),
        onError: (error) => setLocationMessage(error.message),
      },
    )
  }

  return (
    <main className="mx-auto w-full max-w-2xl flex-1 space-y-8 px-4 py-8 sm:px-6">
      <header>
        <h1 className="text-xl font-semibold text-zinc-900">Settings</h1>
        <p className="mt-1 text-sm text-zinc-500">You decide who can see you, and what we keep.</p>
      </header>

      {/* Visibility & audience */}
      <section className="space-y-4 rounded-xl border border-zinc-200 bg-white p-5">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Who can see you</h2>

        <Toggle
          label="Take a break (hide me from the feed)"
          checked={form.paused}
          onChange={(v) => setForm((f) => ({ ...f, paused: v }))}
        />
        <Toggle
          label="Only show me people outside my country"
          checked={form.showOnlyOutsideCountry}
          onChange={(v) => setForm((f) => ({ ...f, showOnlyOutsideCountry: v }))}
        />
        <Toggle
          label="Hide me from people in my own country"
          checked={form.hideFromCountry}
          onChange={(v) => setForm((f) => ({ ...f, hideFromCountry: v }))}
        />
        <Toggle
          label="Women appreciating women only"
          checked={form.womenForWomen}
          onChange={(v) => setForm((f) => ({ ...f, womenForWomen: v }))}
        />
        <Toggle
          label="Only connect with verified people"
          checked={form.verifiedOnly}
          onChange={(v) => setForm((f) => ({ ...f, verifiedOnly: v }))}
        />

        <label className="flex flex-col gap-1 text-sm text-zinc-700">
          <span>Minimum distance from me (km)</span>
          <input
            type="number"
            min={0}
            value={form.minDistanceKm ?? ''}
            onChange={(e) =>
              setForm((f) => ({
                ...f,
                minDistanceKm: e.target.value === '' ? null : Math.max(0, Number(e.target.value)),
              }))
            }
            className="w-32 rounded-md border border-zinc-300 px-3 py-1.5"
            placeholder="off"
          />
          {form.minDistanceKm != null && (
            <span className="text-xs text-amber-700">
              This only takes effect once you share your location below — and it hides people who haven’t shared theirs.
            </span>
          )}
        </label>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={saveVisibility}
            disabled={visibility.isPending}
            className="rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800 disabled:opacity-60"
          >
            {visibility.isPending ? 'Saving…' : 'Save'}
          </button>
          {visibility.isSuccess && <span className="text-sm text-teal-700">Saved.</span>}
          {visibility.isError && <span className="text-sm text-rose-700">{visibility.error.message}</span>}
        </div>
      </section>

      {/* Location */}
      <section className="space-y-3 rounded-xl border border-zinc-200 bg-white p-5">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Location</h2>
        <p className="text-sm text-zinc-500">
          Optional. We round it to roughly 11 km and only ever show distance in broad bands — never your exact spot.
        </p>
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="button"
            onClick={shareLocation}
            disabled={location.isPending}
            className="rounded-md bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 disabled:opacity-60"
          >
            Share my coarse location
          </button>
          <button
            type="button"
            onClick={stopSharingLocation}
            disabled={location.isPending}
            className="rounded-md border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-60"
          >
            Stop sharing
          </button>
        </div>
        {locationMessage && <p className="text-sm text-zinc-600">{locationMessage}</p>}
      </section>

      {/* Blocks */}
      <section className="space-y-3 rounded-xl border border-zinc-200 bg-white p-5">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Blocked people</h2>
        {blocks.isLoading && <p className="text-sm text-zinc-500">Loading…</p>}
        {blocks.isError && <p className="text-sm text-rose-700">Could not load your blocked list.</p>}
        {blocks.data && blocks.data.length === 0 && (
          <p className="text-sm text-zinc-500">You haven’t blocked anyone.</p>
        )}
        {blocks.data && blocks.data.length > 0 && (
          <ul className="divide-y divide-zinc-100">
            {blocks.data.map((b) => (
              <li key={b.profileId} className="flex items-center justify-between py-2">
                <span className="text-sm text-zinc-800">{b.displayName}</span>
                <button
                  type="button"
                  onClick={() => unblock.mutate(b.profileId)}
                  disabled={unblock.isPending}
                  className="text-sm font-medium text-teal-700 hover:text-teal-900 disabled:opacity-60"
                >
                  Unblock
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      {/* Data export */}
      <section className="space-y-3 rounded-xl border border-zinc-200 bg-white p-5">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Your data</h2>
        <p className="text-sm text-zinc-500">Download everything we hold about your account as a JSON file.</p>
        <button
          type="button"
          onClick={() => exportData.mutate()}
          disabled={exportData.isPending}
          className="rounded-md border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-60"
        >
          {exportData.isPending ? 'Preparing…' : 'Download my data'}
        </button>
        {exportData.isError && <p className="text-sm text-rose-700">{exportData.error.message}</p>}
      </section>

      {/* Danger zone */}
      <section className="space-y-3 rounded-xl border border-rose-200 bg-rose-50 p-5">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-rose-700">Delete account</h2>
        {deletion.isSuccess ? (
          <p className="text-sm text-rose-800">
            Your account is scheduled for deletion and you’ve been signed out. It’s fully removed after the grace window.
          </p>
        ) : (
          <>
            <p className="text-sm text-rose-700">
              This hides your account right away and permanently deletes everything after a grace window. It can’t be undone.
            </p>
            {!confirmDelete ? (
              <button
                type="button"
                onClick={() => setConfirmDelete(true)}
                className="rounded-md border border-rose-400 px-4 py-2 text-sm font-medium text-rose-700 hover:bg-rose-100"
              >
                Delete my account
              </button>
            ) : (
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => deletion.mutate()}
                  disabled={deletion.isPending}
                  className="rounded-md bg-rose-700 px-4 py-2 text-sm font-medium text-white hover:bg-rose-800 disabled:opacity-60"
                >
                  {deletion.isPending ? 'Deleting…' : 'Yes, delete everything'}
                </button>
                <button
                  type="button"
                  onClick={() => setConfirmDelete(false)}
                  className="text-sm font-medium text-zinc-600 hover:text-zinc-900"
                >
                  Cancel
                </button>
              </div>
            )}
            {deletion.isError && <p className="text-sm text-rose-800">{deletion.error.message}</p>}
          </>
        )}
      </section>
    </main>
  )
}

function Toggle({
  label,
  checked,
  onChange,
}: {
  label: string
  checked: boolean
  onChange: (value: boolean) => void
}) {
  return (
    <label className="flex items-center justify-between gap-4 text-sm text-zinc-800">
      <span>{label}</span>
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-5 w-5 rounded border-zinc-300 text-teal-700 focus:ring-teal-600"
      />
    </label>
  )
}
