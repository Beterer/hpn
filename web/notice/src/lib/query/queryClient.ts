import { QueryClient } from '@tanstack/react-query'

// Owns all server state; later underpins the feed prefetch queue (backbone §9.2).
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
})
