import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getNotificationSummary,
  markNotificationsSeen,
  type NotificationSummary,
} from '../api/notifications'

export const notificationKeys = {
  summary: ['notifications', 'summary'] as const,
}

export function useNotificationSummary(enabled: boolean) {
  return useQuery<NotificationSummary>({
    queryKey: notificationKeys.summary,
    queryFn: getNotificationSummary,
    enabled,
    refetchInterval: enabled ? 20_000 : false,
    refetchOnWindowFocus: true,
    staleTime: 10_000,
  })
}

export function useMarkNotificationsSeen() {
  const queryClient = useQueryClient()
  return useMutation<void, Error, void>({
    mutationFn: markNotificationsSeen,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.summary })
    },
  })
}
