import { app, type HttpRequest } from '@azure/functions'
import { safeJsonResponse, errorResult, playerAuthorization } from '../lib/http.js'
import { authenticatePlayer } from '../lib/purchases.js'
import { getPathRanking, submitPathResult, mergePathRanking, type PathRankingDependencies } from '../lib/pathRanking.js'
import { AzurePathRankingRepository } from '../lib/pathRankingRepository.js'

const dependencies = (): PathRankingDependencies => ({
  authenticate: authenticatePlayer,
  repository: new AzurePathRankingRepository(),
})
async function body(request: HttpRequest): Promise<unknown> {
  const text = await request.text()
  if (text.length > 4_000) throw new Error('Body too large')
  return JSON.parse(text)
}
app.http('path-result', { methods: ['POST'], authLevel: 'anonymous', route: 'path/results', handler: async request => {
  try { return safeJsonResponse(await submitPathResult(playerAuthorization(request.headers), await body(request), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('path-ranking', { methods: ['GET'], authLevel: 'anonymous', route: 'path/ranking', handler: async request => {
  try { return safeJsonResponse(await getPathRanking(playerAuthorization(request.headers), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('path-merge', { methods: ['POST'], authLevel: 'anonymous', route: 'path/merge', handler: async request => {
  try { return safeJsonResponse(await mergePathRanking(playerAuthorization(request.headers), await body(request), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
