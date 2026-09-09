import { app, type HttpRequest, type HttpResponseInit, type InvocationContext } from '@azure/functions'
import { processAccountDeletion } from '../lib/deleteAccount.js'
import { safeJsonResponse } from '../lib/http.js'
import { deleteMonetizationData, parseAnalyticsIds } from '../lib/deleteMonetizationData.js'

export async function deleteAccount(request: HttpRequest, _context: InvocationContext): Promise<HttpResponseInit> {
  let analyticsIds: string[]
  try {
    const text = await request.text()
    if (text.length > 4000) return { status: 400 }
    analyticsIds = parseAnalyticsIds(text ? JSON.parse(text) : {})
  } catch { return { status: 400 } }
  const result = await processAccountDeletion(request.headers.get('authorization'), process.env, fetch,
    playerId => deleteMonetizationData(playerId, analyticsIds))
  return safeJsonResponse(result)
}

app.http('delete-account', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'account/delete',
  handler: deleteAccount,
})
