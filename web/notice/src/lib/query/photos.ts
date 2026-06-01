import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  deleteProfilePhoto,
  getMyPhotos,
  updatePhotoOrder,
  uploadProfilePhoto,
  type Photo,
} from '../api/photos'

export const photoKeys = {
  mine: ['photos', 'mine'] as const,
}

export function useMyPhotos(enabled = true) {
  return useQuery<Photo[]>({
    queryKey: photoKeys.mine,
    queryFn: getMyPhotos,
    enabled,
    retry: false,
  })
}

export function useUploadProfilePhoto() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => uploadProfilePhoto(file),
    onSuccess: (photo) => {
      queryClient.setQueryData<Photo[]>(photoKeys.mine, (current) =>
        [...(current ?? []), photo].sort((a, b) => Number(a.position) - Number(b.position)),
      )
    },
  })
}

export function useUpdatePhotoOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (photoIds: string[]) => updatePhotoOrder(photoIds),
    onSuccess: (photos) => {
      queryClient.setQueryData(photoKeys.mine, photos)
    },
  })
}

export function useDeleteProfilePhoto() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (photoId: string) => deleteProfilePhoto(photoId),
    onSuccess: (_ignored, photoId) => {
      queryClient.setQueryData<Photo[]>(photoKeys.mine, (current) =>
        (current ?? [])
          .filter((photo) => photo.id !== photoId)
          .map((photo, index) => ({ ...photo, position: index })),
      )
    },
  })
}
