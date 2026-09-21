import { app, type HttpRequest } from '@azure/functions'
import { safeJsonResponse, errorResult } from '../lib/http.js'
import { createPlayerSession } from '../lib/playerSession.js'

async function body(request: HttpRequest): Promise<unknown> {
  const text = await request.text()
  if (text.length > 256) throw new Error('Body too large')
  return JSON.parse(text)
}

app.http('player-session', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'player/session',
  handler: async request => {
    try { return safeJsonResponse(await createPlayerSession(await body(request))) }
    catch { return safeJsonResponse(errorResult(503, 'session_unavailable')) }
  },
})
