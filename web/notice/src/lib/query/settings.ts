import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  blockProfile,
  exportAccount,
  listBlocks,
  requestAccountDeletion,
  unblockProfile,
  updateLocation,
  updateVisibility,
  type BlockedProfile,
  type LocationInput,
  type VisibilityInput,
} from '../api/settings'
import { profileKeys } from './profile'

export const settingsKeys = {
  blocks: ['settings', 'blocks'] as const,
}

export function useBlocks() {
  return useQuery<BlockedProfile[]>({
    queryKey: settingsKeys.blocks,
    queryFn: listBlocks,
  })
}

export function useUpdateVisibility() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: VisibilityInput) => updateVisibility(input),
    onSuccess: () => {
      // The profile response embeds visibility preferences — keep it fresh.
      void queryClient.invalidateQueries({ queryKey: profileKeys.mine })
    },
  })
}

export function useUpdateLocation() {
  return useMutation({
    mutationFn: (input: LocationInput) => updateLocation(input),
  })
}

export function useUnblock() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (profileId: string) => unblockProfile(profileId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: settingsKeys.blocks })
    },
  })
}

export function useBlockProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (targetProfileId: string) => blockProfile(targetProfileId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: settingsKeys.blocks })
    },
  })
}

export function useRequestAccountDeletion() {
  return useMutation({
    mutationFn: () => requestAccountDeletion(),
  })
}

export function useExportAccount() {
  return useMutation({
    mutationFn: async () => {
      const blob = await exportAccount()
      // Trigger a download of the GDPR export bundle.
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'notice-account-export.json'
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    },
  })
}
