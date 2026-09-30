export interface ApiResult {
  status: number
  body: Record<string, unknown>
}

export function errorResult(status: number, code: string): ApiResult {
  return { status, body: { error: code } }
}

export function bearerToken(value: string | null | undefined): string | null {
  if (!value) return null
  const match = /^Bearer\s+([^\s]+)$/i.exec(value)
  return match?.[1] ?? null
}

export function playerAuthorization(headers: { get(name: string): string | null }): string | null {
  // Static Web Apps replaces Authorization with its gateway token.
  return headers.get('x-palabravo-authorization') ?? headers.get('authorization')
}

export function safeJsonResponse(result: ApiResult) {
  return {
    status: result.status,
    jsonBody: result.body,
    headers: {
      'Cache-Control': 'no-store',
      'Content-Type': 'application/json; charset=utf-8',
    },
  }
}
