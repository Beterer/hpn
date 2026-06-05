import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  deleteProfilePhoto,
  getMyPhotos,
  setPrimaryPhoto,
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
    onMutate: async (photoIds) => {
      await queryClient.cancelQueries({ queryKey: photoKeys.mine })
      const previous = queryClient.getQueryData<Photo[]>(photoKeys.mine)

      queryClient.setQueryData<Photo[]>(photoKeys.mine, (current) => {
        const byId = new Map((current ?? []).map((photo) => [photo.id, photo]))
        return photoIds.flatMap((id, position) => {
          const photo = byId.get(id)
          return photo ? [{ ...photo, position }] : []
        })
      })

      return { previous }
    },
    onError: (_error, _photoIds, context) => {
      if (context?.previous) {
        queryClient.setQueryData(photoKeys.mine, context.previous)
      }
    },
    onSuccess: (photos) => {
      queryClient.setQueryData(photoKeys.mine, photos)
    },
  })
}

export function useSetPrimaryPhoto() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (photoId: string) => setPrimaryPhoto(photoId),
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
      queryClient.setQueryData<Photo[]>(photoKeys.mine, (current) => {
        const deletedPrimary = current?.some((photo) => photo.id === photoId && photo.isPrimary) ?? false
        return (current ?? [])
          .filter((photo) => photo.id !== photoId)
          .map((photo, index) => ({
            ...photo,
            position: index,
            isPrimary: deletedPrimary ? index === 0 : photo.isPrimary,
          }))
      })
    },
  })
}
