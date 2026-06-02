import type { components } from '../../lib/api/generated/schema'
import { useMyFingerprint } from '../../lib/query/socialFingerprint'

type DistributionItem = components['schemas']['FingerprintDistributionItemResponse']

export function FingerprintView() {
  const fingerprint = useMyFingerprint()

  if (fingerprint.isLoading) {
    return <CenteredNote>Reading the pattern people have noticed…</CenteredNote>
  }

  if (fingerprint.isError) {
    return (
      <CenteredNote>
        <p className="text-rose-700">{fingerprint.error.message}</p>
        <button
          type="button"
          onClick={() => void fingerprint.refetch()}
          className="mt-3 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 hover:border-teal-700 hover:text-teal-800"
        >
          Try again
        </button>
      </CenteredNote>
    )
  }

  const data = fingerprint.data
  if (!data) {
    return null
  }

  const sampleSize = Number(data.sampleSize)
  const needed = Number(data.needed)
  const isReady = data.status === 'ready'
  const topTraits = data.topTraits ?? []
  const activeDistribution = (data.distribution ?? []).filter((item) => Number(item.share) > 0)

  return (
    <main className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-7 px-4 py-8">
      <section className="border-b border-zinc-200 pb-6">
        <p className="text-sm font-semibold uppercase tracking-widest text-teal-700">Fingerprint</p>
        <h1 className="mt-3 max-w-3xl text-3xl font-semibold text-zinc-950">{data.headline}</h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-zinc-600">{data.summary}</p>
        <p className="mt-4 text-sm font-medium text-zinc-800">
          {isReady
            ? `${sampleSize} appreciation ${sampleSize === 1 ? 'moment' : 'moments'} in this private reading.`
            : `${needed} more appreciation ${needed === 1 ? 'moment' : 'moments'} needed.`}
        </p>
      </section>

      {!isReady ? (
        <section className="max-w-xl py-8">
          <div className="h-2 overflow-hidden rounded-full bg-zinc-200">
            <div
              className="h-full rounded-full bg-teal-700"
              style={{ width: `${Math.min(100, (sampleSize / 20) * 100)}%` }}
            />
          </div>
          <p className="mt-3 text-sm text-zinc-600">{sampleSize} of 20 gathered.</p>
        </section>
      ) : (
        <>
          <section className="grid gap-6 lg:grid-cols-[minmax(0,360px)_1fr]">
            <div className="rounded-lg border border-zinc-200 bg-white p-5 shadow-sm">
              <h2 className="text-sm font-semibold uppercase tracking-widest text-zinc-500">Perception shape</h2>
              <FingerprintRadar distribution={data.distribution ?? []} />
            </div>

            <div className="grid content-start gap-3">
              <h2 className="text-sm font-semibold uppercase tracking-widest text-zinc-500">Recurring traits</h2>
              <div className="grid gap-3 sm:grid-cols-2">
                {topTraits.map((trait) => (
                  <article key={trait.categoryId} className="rounded-lg border border-zinc-200 bg-white p-4 shadow-sm">
                    <div className="flex items-center justify-between gap-3">
                      <h3 className="text-base font-semibold text-zinc-950">{trait.label}</h3>
                      <span className="text-sm font-semibold text-teal-800">{toPercent(trait.share)}</span>
                    </div>
                    <p className="mt-2 text-sm leading-6 text-zinc-600">{trait.phrasing}</p>
                  </article>
                ))}
              </div>
            </div>
          </section>

          <section className="grid gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-widest text-zinc-500">Distribution</h2>
            <div className="grid gap-2">
              {activeDistribution.map((item) => (
                <div key={item.categoryId} className="grid gap-2 rounded-lg border border-zinc-200 bg-white p-4 shadow-sm">
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-sm font-semibold text-zinc-950">{item.label}</span>
                    <span className="text-sm font-semibold text-teal-800">{toPercent(item.share)}</span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-zinc-200">
                    <div className="h-full rounded-full bg-teal-700" style={{ width: toPercent(item.share) }} />
                  </div>
                  <p className="text-sm leading-6 text-zinc-600">{item.phrasing}</p>
                </div>
              ))}
            </div>
          </section>
        </>
      )}
    </main>
  )
}

function FingerprintRadar({ distribution }: { distribution: DistributionItem[] }) {
  const items = distribution.length > 0 ? distribution : []
  const maxShare = Math.max(...items.map((item) => Number(item.share)), 0.01)
  const points = items.map((item, index) => {
    const angle = -Math.PI / 2 + (index / items.length) * Math.PI * 2
    const radius = (Number(item.share) / maxShare) * 46
    return `${Math.cos(angle) * radius},${Math.sin(angle) * radius}`
  })
  const axes = items.map((_, index) => {
    const angle = -Math.PI / 2 + (index / items.length) * Math.PI * 2
    return {
      x: Math.cos(angle) * 50,
      y: Math.sin(angle) * 50,
    }
  })

  return (
    <svg viewBox="-58 -58 116 116" role="img" aria-label="Fingerprint distribution" className="mt-4 aspect-square w-full">
      {[16, 32, 48].map((radius) => (
        <circle key={radius} cx="0" cy="0" r={radius} fill="none" stroke="#d4d4d8" strokeWidth="0.8" />
      ))}
      {axes.map((axis, index) => (
        <line key={index} x1="0" y1="0" x2={axis.x} y2={axis.y} stroke="#e4e4e7" strokeWidth="0.8" />
      ))}
      {points.length > 0 && (
        <polygon points={points.join(' ')} fill="rgba(15, 118, 110, 0.2)" stroke="#0f766e" strokeWidth="2" />
      )}
      {points.map((point, index) => {
        const [x, y] = point.split(',').map(Number)
        return <circle key={items[index].categoryId} cx={x} cy={y} r="2.5" fill="#0f766e" />
      })}
    </svg>
  )
}

function CenteredNote({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16 text-center">{children}</main>
  )
}

function toPercent(value: number | string): string {
  return `${Math.round(Number(value) * 100)}%`
}
