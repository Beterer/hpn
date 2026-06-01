// Thin wrapper over the versioned API. DTO types are generated from the backend's
// OpenAPI document into ./generated/schema.ts via `npm run generate:api` — do not
// hand-write them (backbone §9.2). The session cookie is httpOnly, so requests
// must send credentials and the SPA never touches tokens (§10.1).
export const API_BASE = '/api/v1'

export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  return fetch(`${API_BASE}${path}`, {
    credentials: 'include',
    ...init,
  })
}
