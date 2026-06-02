import { useState, type FormEvent, type ReactNode } from 'react'
import { MagicLinkForm } from '../auth/MagicLinkForm'
import type { AdminQueueItem, AdminReport, AdminStats } from '../../lib/api/admin'
import { useLogout, useMe } from '../../lib/query/auth'
import {
  useAdminQueue,
  useAdminReports,
  useAdminStats,
  useApplyProfileAction,
  useResolveAppeal,
} from '../../lib/query/admin'

const ACTIONS = ['warn', 'temp_restrict', 'ban', 'clear'] as const
const REPORT_STATUSES = ['open', 'reviewing', 'actioned', 'dismissed', 'all'] as const

export function AdminDashboard() {
  const me = useMe()
  const logout = useLogout()
  const isAdmin = me.data?.user.role === 'admin'

  if (me.isLoading) {
    return (
      <AdminFrame>
        <CenteredPanel>Loading...</CenteredPanel>
      </AdminFrame>
    )
  }

  if (!me.data) {
    return (
      <AdminFrame>
        <main className="mx-auto flex min-h-[calc(100vh-65px)] w-full max-w-sm items-center px-4">
          <div className="w-full space-y-4">
            <header>
              <h1 className="text-xl font-semibold text-zinc-950">Admin sign in</h1>
              <p className="mt-1 text-sm text-zinc-500">Use an admin account.</p>
            </header>
            <MagicLinkForm />
          </div>
        </main>
      </AdminFrame>
    )
  }

  if (!isAdmin) {
    return (
      <AdminFrame>
        <CenteredPanel>
          <div className="space-y-3 text-center">
            <h1 className="text-xl font-semibold text-zinc-950">Access denied</h1>
            <p className="text-sm text-zinc-500">{me.data.user.email} is not an admin.</p>
            <button
              type="button"
              onClick={() => logout.mutate()}
              disabled={logout.isPending}
              className="rounded-md border border-zinc-300 px-3 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-60"
            >
              {logout.isPending ? 'Signing out...' : 'Sign out'}
            </button>
          </div>
        </CenteredPanel>
      </AdminFrame>
    )
  }

  return <AdminConsole email={me.data.user.email} />
}

function AdminConsole({ email }: { email: string }) {
  const logout = useLogout()
  const [reportStatus, setReportStatus] = useState<(typeof REPORT_STATUSES)[number]>('open')
  const stats = useAdminStats(true)
  const queue = useAdminQueue(true)
  const reports = useAdminReports(reportStatus, true)

  return (
    <AdminFrame>
      <main className="mx-auto w-full max-w-7xl space-y-6 px-4 py-6 sm:px-6">
        <header className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-200 pb-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-widest text-teal-700">Internal</p>
            <h1 className="text-2xl font-semibold text-zinc-950">Admin</h1>
            <p className="mt-1 text-sm text-zinc-500">{email}</p>
          </div>
          <button
            type="button"
            onClick={() => logout.mutate()}
            disabled={logout.isPending}
            className="rounded-md border border-zinc-300 px-3 py-2 text-sm font-medium text-zinc-700 hover:bg-white disabled:opacity-60"
          >
            {logout.isPending ? 'Signing out...' : 'Sign out'}
          </button>
        </header>

        <StatsStrip stats={stats.data} loading={stats.isLoading} error={stats.error?.message} />

        <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
          <section className="space-y-3 rounded-lg border border-zinc-200 bg-white p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Review queue</h2>
              <button
                type="button"
                onClick={() => queue.refetch()}
                className="rounded-md border border-zinc-300 px-3 py-1.5 text-xs font-medium text-zinc-700 hover:bg-zinc-50"
              >
                Refresh
              </button>
            </div>
            {queue.isLoading && <p className="py-10 text-center text-sm text-zinc-500">Loading queue...</p>}
            {queue.isError && <p className="py-10 text-center text-sm text-rose-700">{queue.error.message}</p>}
            {queue.data && queue.data.length === 0 && (
              <p className="py-10 text-center text-sm text-zinc-500">Queue is empty.</p>
            )}
            {queue.data && queue.data.length > 0 && <QueueTable items={queue.data} />}
          </section>

          <AppealResolver />
        </section>

        <section className="space-y-3 rounded-lg border border-zinc-200 bg-white p-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Reports</h2>
            <select
              value={reportStatus}
              onChange={(event) => setReportStatus(event.target.value as (typeof REPORT_STATUSES)[number])}
              className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-800"
            >
              {REPORT_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {labelize(status)}
                </option>
              ))}
            </select>
          </div>
          {reports.isLoading && <p className="py-10 text-center text-sm text-zinc-500">Loading reports...</p>}
          {reports.isError && <p className="py-10 text-center text-sm text-rose-700">{reports.error.message}</p>}
          {reports.data && reports.data.length === 0 && (
            <p className="py-10 text-center text-sm text-zinc-500">No reports.</p>
          )}
          {reports.data && reports.data.length > 0 && <ReportsTable reports={reports.data} />}
        </section>
      </main>
    </AdminFrame>
  )
}

function StatsStrip({
  stats,
  loading,
  error,
}: {
  stats: AdminStats | undefined
  loading: boolean
  error: string | undefined
}) {
  if (loading) {
    return <section className="rounded-lg border border-zinc-200 bg-white p-4 text-sm text-zinc-500">Loading stats...</section>
  }

  if (error || !stats) {
    return <section className="rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">{error}</section>
  }

  return (
    <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <Stat label="Open" value={stats.openReports} />
      <Stat label="Reviewing" value={stats.reviewingReports} />
      <Stat label="Actioned" value={stats.actionedReports} />
      <Stat label="Dismissed" value={stats.dismissedReports} />
      <Stat label="Restricted" value={stats.currentlyRestricted} tone="amber" />
      <Stat label="Banned" value={stats.currentlyBanned} tone="rose" />
      <Stat label="Verified" value={stats.verifiedProfiles} />
      <Stat label="Avg trust" value={Number(stats.averageTrustScore).toFixed(2)} />
    </section>
  )
}

function Stat({ label, value, tone = 'zinc' }: { label: string; value: number | string; tone?: 'zinc' | 'amber' | 'rose' }) {
  const toneClass =
    tone === 'amber'
      ? 'border-amber-200 bg-amber-50 text-amber-900'
      : tone === 'rose'
        ? 'border-rose-200 bg-rose-50 text-rose-900'
        : 'border-zinc-200 bg-white text-zinc-950'

  return (
    <div className={`rounded-lg border p-4 ${toneClass}`}>
      <p className="text-xs font-semibold uppercase tracking-wide opacity-70">{label}</p>
      <p className="mt-1 text-2xl font-semibold">{value}</p>
    </div>
  )
}

function QueueTable({ items }: { items: AdminQueueItem[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-[900px] w-full border-separate border-spacing-0 text-left text-sm">
        <thead className="text-xs uppercase tracking-wide text-zinc-500">
          <tr>
            <th className="border-b border-zinc-200 px-3 py-2">Profile</th>
            <th className="border-b border-zinc-200 px-3 py-2">Status</th>
            <th className="border-b border-zinc-200 px-3 py-2">Reports</th>
            <th className="border-b border-zinc-200 px-3 py-2">Trust</th>
            <th className="border-b border-zinc-200 px-3 py-2">Latest</th>
            <th className="border-b border-zinc-200 px-3 py-2">Action</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.profileId} className="align-top">
              <td className="border-b border-zinc-100 px-3 py-3">
                <p className="font-medium text-zinc-950">{item.displayName}</p>
                <p className="mt-1 max-w-48 truncate font-mono text-xs text-zinc-500">{item.profileId}</p>
              </td>
              <td className="border-b border-zinc-100 px-3 py-3">{labelize(item.profileStatus)}</td>
              <td className="border-b border-zinc-100 px-3 py-3">{item.reportCount}</td>
              <td className="border-b border-zinc-100 px-3 py-3">{Number(item.trustScore).toFixed(2)}</td>
              <td className="border-b border-zinc-100 px-3 py-3">{formatDate(item.latestReportAt)}</td>
              <td className="border-b border-zinc-100 px-3 py-3">
                <ProfileActionForm profileId={item.profileId} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function ProfileActionForm({ profileId }: { profileId: string }) {
  const applyAction = useApplyProfileAction()
  const [action, setAction] = useState<(typeof ACTIONS)[number]>('warn')
  const [reason, setReason] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    applyAction.mutate(
      { profileId, request: { action, reason } },
      {
        onSuccess: () => setReason(''),
      },
    )
  }

  return (
    <form onSubmit={submit} className="grid min-w-[320px] grid-cols-[130px_minmax(120px,1fr)_auto] gap-2">
      <select
        value={action}
        onChange={(event) => setAction(event.target.value as (typeof ACTIONS)[number])}
        className="rounded-md border border-zinc-300 px-2 py-1.5 text-sm text-zinc-800"
      >
        {ACTIONS.map((value) => (
          <option key={value} value={value}>
            {labelize(value)}
          </option>
        ))}
      </select>
      <input
        value={reason}
        onChange={(event) => setReason(event.target.value)}
        minLength={3}
        maxLength={500}
        required
        placeholder="Reason"
        className="rounded-md border border-zinc-300 px-2 py-1.5 text-sm text-zinc-800"
      />
      <button
        type="submit"
        disabled={applyAction.isPending}
        className="rounded-md bg-zinc-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-zinc-700 disabled:opacity-60"
      >
        {applyAction.isPending ? 'Saving' : 'Apply'}
      </button>
      {applyAction.isError && (
        <p className="col-span-3 text-xs text-rose-700">{applyAction.error.message}</p>
      )}
      {applyAction.isSuccess && (
        <p className="col-span-3 text-xs text-teal-700">Audit {applyAction.data.auditId}</p>
      )}
    </form>
  )
}

function ReportsTable({ reports }: { reports: AdminReport[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-[960px] w-full border-separate border-spacing-0 text-left text-sm">
        <thead className="text-xs uppercase tracking-wide text-zinc-500">
          <tr>
            <th className="border-b border-zinc-200 px-3 py-2">Target</th>
            <th className="border-b border-zinc-200 px-3 py-2">Type</th>
            <th className="border-b border-zinc-200 px-3 py-2">Status</th>
            <th className="border-b border-zinc-200 px-3 py-2">Reporter</th>
            <th className="border-b border-zinc-200 px-3 py-2">Created</th>
            <th className="border-b border-zinc-200 px-3 py-2">Note</th>
          </tr>
        </thead>
        <tbody>
          {reports.map((report) => (
            <tr key={report.reportId} className="align-top">
              <td className="border-b border-zinc-100 px-3 py-3">
                <p className="font-medium text-zinc-950">{report.targetDisplayName ?? 'Unknown'}</p>
                <p className="mt-1 max-w-48 truncate font-mono text-xs text-zinc-500">{report.targetProfileId}</p>
              </td>
              <td className="border-b border-zinc-100 px-3 py-3">{labelize(report.type)}</td>
              <td className="border-b border-zinc-100 px-3 py-3">{labelize(report.status)}</td>
              <td className="border-b border-zinc-100 px-3 py-3 font-mono text-xs text-zinc-500">
                {report.reporterUserId}
              </td>
              <td className="border-b border-zinc-100 px-3 py-3">{formatDate(report.createdAt)}</td>
              <td className="border-b border-zinc-100 px-3 py-3 text-zinc-600">{report.note ?? '-'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function AppealResolver() {
  const resolve = useResolveAppeal()
  const [appealId, setAppealId] = useState('')
  const [targetProfileId, setTargetProfileId] = useState('')
  const [outcome, setOutcome] = useState<'upheld' | 'dismissed'>('dismissed')
  const [note, setNote] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    resolve.mutate(
      { appealId, request: { targetProfileId, outcome, note } },
      {
        onSuccess: () => {
          setAppealId('')
          setTargetProfileId('')
          setNote('')
        },
      },
    )
  }

  return (
    <section className="space-y-3 rounded-lg border border-zinc-200 bg-white p-4">
      <h2 className="text-sm font-semibold uppercase tracking-wide text-zinc-700">Appeals</h2>
      <p className="text-xs text-zinc-500">An upheld appeal lifts the profile&apos;s restriction or ban.</p>
      <form onSubmit={submit} className="space-y-3">
        <label className="block text-sm text-zinc-700">
          <span className="mb-1 block font-medium">Appeal id</span>
          <input
            value={appealId}
            onChange={(event) => setAppealId(event.target.value)}
            required
            className="w-full rounded-md border border-zinc-300 px-3 py-2 font-mono text-sm text-zinc-800"
          />
        </label>
        <label className="block text-sm text-zinc-700">
          <span className="mb-1 block font-medium">Target profile id</span>
          <input
            value={targetProfileId}
            onChange={(event) => setTargetProfileId(event.target.value)}
            required
            className="w-full rounded-md border border-zinc-300 px-3 py-2 font-mono text-sm text-zinc-800"
          />
        </label>
        <label className="block text-sm text-zinc-700">
          <span className="mb-1 block font-medium">Outcome</span>
          <select
            value={outcome}
            onChange={(event) => setOutcome(event.target.value as 'upheld' | 'dismissed')}
            className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm text-zinc-800"
          >
            <option value="dismissed">Dismissed</option>
            <option value="upheld">Upheld</option>
          </select>
        </label>
        <label className="block text-sm text-zinc-700">
          <span className="mb-1 block font-medium">Note</span>
          <textarea
            value={note}
            onChange={(event) => setNote(event.target.value)}
            minLength={3}
            maxLength={500}
            required
            rows={4}
            className="w-full resize-y rounded-md border border-zinc-300 px-3 py-2 text-sm text-zinc-800"
          />
        </label>
        <button
          type="submit"
          disabled={resolve.isPending}
          className="w-full rounded-md bg-zinc-900 px-3 py-2 text-sm font-medium text-white hover:bg-zinc-700 disabled:opacity-60"
        >
          {resolve.isPending ? 'Recording...' : 'Resolve'}
        </button>
        {resolve.isError && <p className="text-sm text-rose-700">{resolve.error.message}</p>}
        {resolve.isSuccess && (
          <p className="text-sm text-teal-700">
            {resolve.data.restrictionLifted ? 'Restriction lifted. ' : ''}Audit {resolve.data.auditId}
          </p>
        )}
      </form>
    </section>
  )
}

function AdminFrame({ children }: { children: ReactNode }) {
  return <div className="min-h-full bg-zinc-50 text-zinc-900">{children}</div>
}

function CenteredPanel({ children }: { children: ReactNode }) {
  return <main className="flex min-h-screen items-center justify-center px-4">{children}</main>
}

function labelize(value: string) {
  return value.replaceAll('_', ' ').replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
