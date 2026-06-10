import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getMe, logout, startGuestSession, type Me } from '../api/auth'
import { photoKeys } from './photos'
import { profileKeys } from './profile'

export const authKeys = {
  me: ['me'] as const,
  guestSession: ['guest', 'session'] as const,
}

const guestActiveKey = 'notice.guest.active'
const guestThresholdKey = 'notice.guest.nudgeThreshold'
const guestProfilesSeenKey = 'notice.guest.profilesSeen'
const guestLastNudgeDismissedAtKey = 'notice.guest.lastNudgeDismissedAt'

/**
 * Establishes session + onboarding state on load (backbone §9.2). A null result
 * is a normal "signed out" state, not an error, so the UI can branch on it.
 */
export function useMe() {
  return useQuery<Me | null>({
    queryKey: authKeys.me,
    queryFn: getMe,
    staleTime: 60_000,
    retry: false,
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: logout,
    onSuccess: () => {
      clearGuestState()
      queryClient.setQueryData(authKeys.me, null)
      queryClient.removeQueries({ queryKey: authKeys.guestSession })
      queryClient.removeQueries({ queryKey: profileKeys.mine })
      queryClient.removeQueries({ queryKey: photoKeys.mine })
    },
  })
}

export function readGuestActive(): boolean {
  return localStorage.getItem(guestActiveKey) === 'true'
}

export function clearGuestState() {
  localStorage.removeItem(guestActiveKey)
  localStorage.removeItem(guestThresholdKey)
  localStorage.removeItem(guestProfilesSeenKey)
  localStorage.removeItem(guestLastNudgeDismissedAtKey)
}

function persistGuestSession(session: { nudgeThreshold: unknown }) {
  localStorage.setItem(guestActiveKey, 'true')
  localStorage.setItem(guestThresholdKey, String(Number(session.nudgeThreshold)))
  localStorage.setItem(guestProfilesSeenKey, localStorage.getItem(guestProfilesSeenKey) ?? '0')
}

export function useEnsureGuestSession(enabled: boolean) {
  return useQuery({
    queryKey: authKeys.guestSession,
    queryFn: async () => {
      const session = await startGuestSession()
      persistGuestSession(session)
      return session
    },
    enabled,
    staleTime: Infinity,
    retry: false,
  })
}

export function useGuestSession() {
  const start = useMutation({
    mutationFn: startGuestSession,
    onSuccess: persistGuestSession,
  })

  return {
    start,
    markActive(threshold: number) {
      localStorage.setItem(guestActiveKey, 'true')
      localStorage.setItem(guestThresholdKey, String(threshold))
    },
    lastNudgeDismissedAt() {
      return Number(localStorage.getItem(guestLastNudgeDismissedAtKey) ?? '0')
    },
    shouldShowNudge(reactionCount: number, lastDismissedAt: number) {
      const threshold = Number(localStorage.getItem(guestThresholdKey) ?? '10')
      return reactionCount > 0 && reactionCount % threshold === 0 && lastDismissedAt !== reactionCount
    },
    reactionCount() {
      return Number(localStorage.getItem(guestProfilesSeenKey) ?? '0')
    },
    recordReaction() {
      const next = Number(localStorage.getItem(guestProfilesSeenKey) ?? '0') + 1
      localStorage.setItem(guestProfilesSeenKey, String(next))
      return next
    },
    dismissNudge(reactionCount: number) {
      localStorage.setItem(guestLastNudgeDismissedAtKey, String(reactionCount))
    },
    clear: clearGuestState,
  }
}
