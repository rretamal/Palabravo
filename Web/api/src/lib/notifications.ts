import { createHash, timingSafeEqual } from 'node:crypto'
import { GoogleAuth } from 'google-auth-library'
import { bearerToken, errorResult, type ApiResult } from './http.js'

const idPattern = /^[a-z0-9][a-z0-9-]{2,63}$/
const topic = 'new-challenges-es'

export interface NewChallengeNotification {
  weeklyId: string
  title: string
  body: string
}

export interface NotificationSender {
  send(notification: NewChallengeNotification): Promise<string>
}

export interface NotificationDependencies {
  adminKey: string
  sender: NotificationSender
}

const cleanText = (value: unknown, max: number): string | null => {
  if (typeof value !== 'string') return null
  const clean = value.trim().replace(/[\u0000-\u001f\u007f]/g, '')
  return clean.length > 0 && clean.length <= max ? clean : null
}

export function parseNewChallengeNotification(value: unknown): NewChallengeNotification | null {
  if (!value || typeof value !== 'object') return null
  const item = value as Record<string, unknown>
  const weeklyId = cleanText(item.weeklyId, 64)
  const title = cleanText(item.title, 80)
  const body = cleanText(item.body, 180)
  return weeklyId && idPattern.test(weeklyId) && title && body ? { weeklyId, title, body } : null
}

function matchesSecret(candidate: string | null, expected: string): boolean {
  if (!candidate || !expected) return false
  const left = createHash('sha256').update(candidate).digest()
  const right = createHash('sha256').update(expected).digest()
  return timingSafeEqual(left, right)
}

export async function sendNewChallengeNotification(authorization: string | null, value: unknown,
  deps: NotificationDependencies): Promise<ApiResult> {
  if (!matchesSecret(bearerToken(authorization), deps.adminKey)) return errorResult(401, 'invalid_admin_key')
  const notification = parseNewChallengeNotification(value)
  if (!notification) return errorResult(400, 'invalid_notification')
  const messageId = await deps.sender.send(notification)
  return { status: 202, body: { accepted: true, messageId } }
}

const required = (name: string): string => {
  const value = process.env[name]
  if (!value) throw new Error(`${name} not configured`)
  return value
}

export class FcmTopicSender implements NotificationSender {
  async send(notification: NewChallengeNotification): Promise<string> {
    const credentials = JSON.parse(required('FIREBASE_SERVICE_ACCOUNT_JSON')) as { project_id?: string }
    if (!credentials.project_id || !/^[a-z0-9][a-z0-9-]{3,61}[a-z0-9]$/.test(credentials.project_id))
      throw new Error('Invalid Firebase project')
    const auth = new GoogleAuth({ credentials, scopes: ['https://www.googleapis.com/auth/firebase.messaging'] })
    const client = await auth.getClient()
    const response = await client.request<{ name?: string }>({
      url: `https://fcm.googleapis.com/v1/projects/${credentials.project_id}/messages:send`,
      method: 'POST',
      timeout: 10_000,
      data: {
        message: {
          topic,
          notification: { title: notification.title, body: notification.body },
          data: { weeklyId: notification.weeklyId, link: 'https://palabravo.app/semanal' },
          android: { priority: 'high', notification: { channel_id: 'palabravo_updates' } },
          apns: { payload: { aps: { sound: 'default' } } },
        },
      },
    })
    return response.data.name ?? 'accepted'
  }
}
