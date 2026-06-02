import { useMutation } from '@tanstack/react-query'
import { submitReport } from '../api/reports'

export function useSubmitReport() {
  return useMutation({
    mutationFn: (input: { targetProfileId: string; type: string; note?: string | null }) =>
      submitReport(input),
  })
}
