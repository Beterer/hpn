import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  applyProfileAction,
  getAdminQueue,
  getAdminReports,
  getAdminStats,
  resolveAppeal,
  type ApplyProfileActionRequest,
  type ResolveAppealRequest,
} from '../api/admin'

export const adminKeys = {
  all: ['admin'] as const,
  queue: ['admin', 'queue'] as const,
  reports: (status: string) => ['admin', 'reports', status] as const,
  stats: ['admin', 'stats'] as const,
}

export function useAdminQueue(enabled: boolean) {
  return useQuery({
    queryKey: adminKeys.queue,
    queryFn: () => getAdminQueue(),
    enabled,
  })
}

export function useAdminReports(status: string, enabled: boolean) {
  return useQuery({
    queryKey: adminKeys.reports(status),
    queryFn: () => getAdminReports({ status }),
    enabled,
  })
}

export function useAdminStats(enabled: boolean) {
  return useQuery({
    queryKey: adminKeys.stats,
    queryFn: getAdminStats,
    enabled,
  })
}

export function useApplyProfileAction() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { profileId: string; request: ApplyProfileActionRequest }) =>
      applyProfileAction(input.profileId, input.request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: adminKeys.all })
    },
  })
}

export function useResolveAppeal() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { appealId: string; request: ResolveAppealRequest }) =>
      resolveAppeal(input.appealId, input.request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: adminKeys.all })
    },
  })
}
