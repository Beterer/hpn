import { useMutation, useQuery } from '@tanstack/react-query'
import {
  getAppreciationCategories,
  submitAppreciation,
  type AppreciationCategory,
  type SubmitAppreciationRequest,
  type SubmitAppreciationResponse,
} from '../api/appreciation'

export const appreciationKeys = {
  categories: ['appreciation-categories'] as const,
}

export function useAppreciationCategories() {
  return useQuery<AppreciationCategory[]>({
    queryKey: appreciationKeys.categories,
    queryFn: getAppreciationCategories,
    staleTime: 10 * 60_000,
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
