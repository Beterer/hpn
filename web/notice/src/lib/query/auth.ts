import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getMe, logout, type Me } from '../api/auth'
import { photoKeys } from './photos'
import { profileKeys } from './profile'

export const authKeys = {
  me: ['me'] as const,
}

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
      queryClient.setQueryData(authKeys.me, null)
      queryClient.removeQueries({ queryKey: profileKeys.mine })
      queryClient.removeQueries({ queryKey: photoKeys.mine })
    },
  })
}
