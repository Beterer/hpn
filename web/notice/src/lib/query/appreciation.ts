import { useMutation, useQuery } from '@tanstack/react-query'
import {
  getAppreciationCategories,
  getReceivedAppreciation,
  submitAppreciation,
  type AppreciationCategory,
  type ReceivedAppreciation,
  type SubmitAppreciationRequest,
  type SubmitAppreciationResponse,
} from '../api/appreciation'

export const appreciationKeys = {
  categories: ['appreciation-categories'] as const,
  received: (includeEvents: boolean) => ['appreciations', 'received', includeEvents] as const,
}

export function useAppreciationCategories() {
  return useQuery<AppreciationCategory[]>({
    queryKey: appreciationKeys.categories,
    queryFn: getAppreciationCategories,
    staleTime: 10 * 60_000,
  })
}

export function useReceivedAppreciation(includeEvents = true) {
  return useQuery<ReceivedAppreciation>({
    queryKey: appreciationKeys.received(includeEvents),
    queryFn: () => getReceivedAppreciation(includeEvents),
    staleTime: 60_000,
  })
}

export function useSubmitAppreciation() {
  return useMutation<
    SubmitAppreciationResponse,
    Error,
    { request: SubmitAppreciationRequest; idempotencyKey?: string }
  >({
    mutationFn: ({ request, idempotencyKey }) => submitAppreciation(request, idempotencyKey),
  })
}
