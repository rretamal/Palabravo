import { app, type HttpRequest } from '@azure/functions'
import { errorResult, safeJsonResponse } from '../lib/http.js'
import { FcmTopicSender, sendNewChallengeNotification } from '../lib/notifications.js'

async function body(request: HttpRequest): Promise<unknown> {
  const text = await request.text()
  if (text.length > 4_000) throw new Error('Body too large')
  return JSON.parse(text)
}

app.http('new-challenge-notification', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'notifications/new-challenge',
  handler: async request => {
    try {
      return safeJsonResponse(await sendNewChallengeNotification(
        request.headers.get('authorization'), await body(request), {
          adminKey: process.env.NOTIFICATION_ADMIN_KEY ?? '',
          sender: new FcmTopicSender(),
        }))
    } catch {
      return safeJsonResponse(errorResult(503, 'notification_unavailable'))
    }
  },
})
