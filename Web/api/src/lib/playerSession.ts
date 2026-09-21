import { errorResult, type ApiResult } from './http.js'

interface SessionEnvironment {
  PLAYFAB_TITLE_ID?: string
  PLAYFAB_SECRET_KEY?: string
}

const customIdPattern = /^[a-f0-9]{32}$/

export async function createPlayerSession(body: unknown,
  environment: SessionEnvironment = process.env,
  fetchImpl: typeof fetch = fetch): Promise<ApiResult> {
  const customId = body && typeof body === 'object'
    ? (body as Record<string, unknown>).customId : null
  if (typeof customId !== 'string' || !customIdPattern.test(customId))
    return errorResult(400, 'invalid_custom_id')

  const titleId = environment.PLAYFAB_TITLE_ID?.trim()
  const secretKey = environment.PLAYFAB_SECRET_KEY?.trim()
  if (!titleId || !/^[A-Za-z0-9]+$/.test(titleId) || !secretKey)
    return errorResult(503, 'playfab_not_configured')

  const response = await fetchImpl(`https://${titleId}.playfabapi.com/Server/LoginWithCustomID`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-SecretKey': secretKey },
    body: JSON.stringify({ CustomId: customId, CreateAccount: true }),
    signal: AbortSignal.timeout(10_000),
  })
  if (!response.ok) return errorResult(502, 'playfab_login_failed')

  const envelope = await response.json() as {
    data?: {
      SessionTicket?: string
      EntityToken?: {
        EntityToken?: string
        TokenExpiration?: string
        Entity?: { Id?: string; Type?: string }
      }
    }
  }
  const data = envelope.data
  if (!data?.SessionTicket) return errorResult(502, 'invalid_playfab_response')
  return {
    status: 200,
    body: {
      sessionTicket: data.SessionTicket,
      entityToken: data.EntityToken ? {
        entityToken: data.EntityToken.EntityToken,
        tokenExpiration: data.EntityToken.TokenExpiration,
        entity: data.EntityToken.Entity ? {
          id: data.EntityToken.Entity.Id,
          type: data.EntityToken.Entity.Type,
        } : null,
      } : null,
    },
  }
}
