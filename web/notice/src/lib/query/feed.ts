import { useCallback, useEffect, useRef, useState } from 'react'
import { getFeedNext, type FeedProfile } from '../api/feed'

const BATCH_SIZE = 10
// Refill in the background once the local queue runs this low, so the gated
// browse loop (M5) serves the next card from cache without a server round-trip.
const REFILL_THRESHOLD = 3

type FeedStatus = 'loading' | 'ready' | 'error'

export interface FeedQueue {
  current: FeedProfile | null
  /** The next card, shown scaled-back behind the current one for depth. */
  next: FeedProfile | null
  remaining: number
  status: FeedStatus
  error: string | null
  exhausted: boolean
  /** Drop the current card. M5's appreciation submit is the only caller. */
  advance: () => void
  reload: () => void
}

/**
 * The feed prefetch queue (backbone §3.4, §9.2). Holds a local batch of eligible
 * profiles, serves the head as the current card, and refills in the background as
 * it drains. Session-level dedupe is the client's job: every fetched profile is
 * remembered and passed back as `seen`, so the server never resends it and there
 * is no impressions table (§7.6).
 */
export function useFeedQueue(): FeedQueue {
  const [queue, setQueue] = useState<FeedProfile[]>([])
  const [status, setStatus] = useState<FeedStatus>('loading')
  const [error, setError] = useState<string | null>(null)
  const [exhausted, setExhausted] = useState(false)

  const seenRef = useRef<Set<string>>(new Set())
  const inFlightRef = useRef(false)
  const exhaustedRef = useRef(false)

  const refill = useCallback(async () => {
    if (inFlightRef.current || exhaustedRef.current) {
      return
    }
    inFlightRef.current = true
    try {
      const batch = await getFeedNext({ limit: BATCH_SIZE, seen: [...seenRef.current] })
      setQueue((prev) => {
        const known = new Set(prev.map((p) => p.profileId))
        const fresh = batch.filter((p) => p.profileId && !known.has(p.profileId))
        for (const profile of fresh) {
          if (profile.profileId) {
            seenRef.current.add(profile.profileId)
          }
        }
        if (fresh.length === 0) {
          exhaustedRef.current = true
          setExhausted(true)
        }
        return [...prev, ...fresh]
      })
      setStatus('ready')
    } catch (err) {
      setError((err as Error).message)
      setStatus('error')
    } finally {
      inFlightRef.current = false
    }
  }, [])

  const reload = useCallback(() => {
    seenRef.current.clear()
    exhaustedRef.current = false
    setExhausted(false)
    setError(null)
    setStatus('loading')
    setQueue([])
    void refill()
  }, [refill])

  // Initial load.
  useEffect(() => {
    void refill()
  }, [refill])

  // Background refill as the queue drains.
  useEffect(() => {
    if (status === 'ready' && !exhaustedRef.current && queue.length <= REFILL_THRESHOLD) {
      void refill()
    }
  }, [status, queue.length, refill])

  const advance = useCallback(() => {
    setQueue((prev) => prev.slice(1))
  }, [])

  return {
    current: queue[0] ?? null,
    next: queue[1] ?? null,
    remaining: queue.length,
    status,
    error,
    exhausted,
    advance,
    reload,
  }
}
