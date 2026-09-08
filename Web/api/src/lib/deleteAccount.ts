import { bearerToken, errorResult, type ApiResult } from './http.js'

interface Environment {
  PLAYFAB_TITLE_ID?: string
  PLAYFAB_SECRET_KEY?: string
}

interface PlayFabEnvelope<T> {
  data?: T
}

export async function processAccountDeletion(
  authorization: string | null | undefined,
  environment: Environment = process.env,
  fetchImpl: typeof fetch = fetch,
): Promise<ApiResult> {
  const sessionTicket = bearerToken(authorization)
  if (!sessionTicket) return errorResult(401, 'authentication_required')

  const titleId = environment.PLAYFAB_TITLE_ID?.trim()
  const secretKey = environment.PLAYFAB_SECRET_KEY?.trim()
  if (!titleId || !secretKey) return errorResult(503, 'service_unavailable')

  try {
    const playerResponse = await fetchImpl(`https://${titleId}.playfabapi.com/Admin/GetPlayerIdFromAuthToken`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-SecretKey': secretKey },
      body: JSON.stringify({ Token: sessionTicket, TokenType: 'SessionTicket' }),
    })
    if (!playerResponse.ok) return errorResult(401, 'invalid_session')

    const playerEnvelope = await playerResponse.json() as PlayFabEnvelope<{ PlayFabId?: string }>
    const playFabId = playerEnvelope.data?.PlayFabId
    if (!playFabId) return errorResult(401, 'invalid_session')

    const deletionResponse = await fetchImpl(`https://${titleId}.playfabapi.com/Admin/DeleteMasterPlayerAccount`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-SecretKey': secretKey },
      body: JSON.stringify({ PlayFabId: playFabId, MetaData: 'Authenticated in-app deletion request' }),
    })
    if (!deletionResponse.ok) return errorResult(502, 'deletion_not_accepted')

    const deletionEnvelope = await deletionResponse.json() as PlayFabEnvelope<{ JobReceiptId?: string }>
    const requestId = deletionEnvelope.data?.JobReceiptId
    if (!requestId) return errorResult(502, 'deletion_not_accepted')

    return { status: 202, body: { requestId, status: 'queued' } }
  } catch {
    return errorResult(503, 'service_unavailable')
  }
}
