import { useQuery } from '@tanstack/react-query'
import { getMyFingerprint, type SocialFingerprint } from '../api/socialFingerprint'

export const socialFingerprintKeys = {
  mine: ['fingerprint', 'me'] as const,
}

export function useMyFingerprint() {
  return useQuery<SocialFingerprint>({
    queryKey: socialFingerprintKeys.mine,
    queryFn: getMyFingerprint,
    staleTime: 60_000,
  })
}
