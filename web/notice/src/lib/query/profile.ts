import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getInterests,
  getMyProfile,
  updateProfileInterests,
  updateProfileStatus,
  upsertProfile,
  type Profile,
  type UpsertProfileInput,
} from '../api/profile'

export const profileKeys = {
  mine: ['profile', 'mine'] as const,
  interests: ['profile', 'interests'] as const,
}

export function useMyProfile() {
  return useQuery<Profile | null>({
    queryKey: profileKeys.mine,
    queryFn: getMyProfile,
    retry: false,
  })
}

export function useInterests() {
  return useQuery({
    queryKey: profileKeys.interests,
    queryFn: getInterests,
    staleTime: 60 * 60 * 1000,
  })
}

export function useUpsertProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: UpsertProfileInput) => upsertProfile(input),
    onSuccess: (profile) => {
      queryClient.setQueryData(profileKeys.mine, profile)
    },
  })
}

export function useUpdateProfileInterests() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (interestIds: string[]) => updateProfileInterests(interestIds),
    onSuccess: (profile) => {
      queryClient.setQueryData(profileKeys.mine, profile)
    },
  })
}

export function useUpdateProfileStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (status: 'active' | 'paused') => updateProfileStatus(status),
    onSuccess: (profile) => {
      queryClient.setQueryData(profileKeys.mine, profile)
    },
  })
}
