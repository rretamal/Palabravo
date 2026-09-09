import { app, type HttpRequest } from '@azure/functions'
import { authenticatePlayer } from '../lib/purchases.js'
import { AzureWeeklyRepository } from '../lib/weeklyRepository.js'
import { createWeeklyInvite, getWeeklyRanking, resolveWeeklyInvite, submitWeekly,
  type WeeklyDependencies } from '../lib/weekly.js'
import { errorResult, safeJsonResponse } from '../lib/http.js'

const dependencies = (): WeeklyDependencies => ({ authenticate: authenticatePlayer, repository: new AzureWeeklyRepository() })
async function body(request: HttpRequest): Promise<unknown> {
  const text = await request.text()
  if (text.length > 8_000) throw new Error('Body too large')
  return JSON.parse(text)
}
app.http('weekly-result', { methods: ['POST'], authLevel: 'anonymous', route: 'weekly/results', handler: async request => {
  try { return safeJsonResponse(await submitWeekly(request.headers.get('authorization'), await body(request), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('weekly-ranking', { methods: ['GET'], authLevel: 'anonymous', route: 'weekly/{id}/ranking', handler: async request => {
  try { return safeJsonResponse(await getWeeklyRanking(request.headers.get('authorization'), request.params.id, dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('weekly-challenge-create', { methods: ['POST'], authLevel: 'anonymous', route: 'challenges', handler: async request => {
  try { return safeJsonResponse(await createWeeklyInvite(request.headers.get('authorization'), await body(request), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('weekly-challenge-get', { methods: ['GET'], authLevel: 'anonymous', route: 'challenges/{token}', handler: async request => {
  try { return safeJsonResponse(await resolveWeeklyInvite(request.params.token.toUpperCase(), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
