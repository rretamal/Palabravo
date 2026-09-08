import { errorResult, type ApiResult } from './http.js'

export interface DeletionRequestBody {
  email?: unknown
  identifierType?: unknown
  identifier?: unknown
  details?: unknown
  locale?: unknown
  website?: unknown
  startedAt?: unknown
  consent?: unknown
}

interface Environment {
  RESEND_API_KEY?: string
  RESEND_FROM?: string
  PRIVACY_REQUEST_TO?: string
  PUBLIC_SITE_URL?: string
  AZURE_FUNCTIONS_ENVIRONMENT?: string
}

interface RequestMetadata {
  origin?: string | null
  contentLength?: string | null
}

function text(value: unknown, max: number): string {
  return typeof value === 'string' ? value.trim().slice(0, max) : ''
}

function validEmail(value: string): boolean {
  return value.length <= 160 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
}

function originAllowed(origin: string | null | undefined, environment: Environment): boolean {
  const publicUrl = environment.PUBLIC_SITE_URL
  if (!publicUrl) return false
  try {
    const expected = new URL(publicUrl).origin
    if (origin === expected) return true
    return environment.AZURE_FUNCTIONS_ENVIRONMENT === 'Development'
      && (origin === 'http://localhost:4280' || Boolean(origin?.startsWith('http://127.0.0.1:')))
  } catch {
    return false
  }
}

function emails(body: Required<Pick<DeletionRequestBody, 'email' | 'identifierType' | 'identifier' | 'details' | 'locale'>>, requestId: string, environment: Environment) {
  const locale = body.locale === 'en' ? 'en' : 'es'
  const userSubject = locale === 'es' ? `Solicitud de eliminación ${requestId}` : `Deletion request ${requestId}`
  const userText = locale === 'es'
    ? `Recibimos tu solicitud de eliminación de Palabravo.\n\nReferencia: ${requestId}\n\nLa atenderemos en un máximo de 30 días. Es posible que respondamos a este correo para verificar la cuenta. Nunca te pediremos una contraseña ni un código de acceso.`
    : `We received your Palabravo deletion request.\n\nReference: ${requestId}\n\nWe will handle it within 30 days. We may reply to verify the account. We will never ask for a password or access code.`
  const adminText = [
    'Nueva solicitud manual de eliminación de Palabravo',
    `Referencia: ${requestId}`,
    `Correo: ${body.email}`,
    `Tipo de identificador: ${body.identifierType}`,
    `Identificador: ${body.identifier}`,
    `Idioma: ${locale}`,
    `Detalles: ${body.details || '(sin detalles)'}`,
    `Recibida UTC: ${new Date().toISOString()}`,
  ].join('\n')

  return [
    { from: environment.RESEND_FROM, to: [environment.PRIVACY_REQUEST_TO], reply_to: body.email, subject: `[Palabravo] Borrado ${requestId}`, text: adminText },
    { from: environment.RESEND_FROM, to: [body.email], reply_to: environment.PRIVACY_REQUEST_TO, subject: userSubject, text: userText },
  ]
}

export async function processDeletionRequest(
  body: DeletionRequestBody,
  metadata: RequestMetadata,
  environment: Environment = process.env,
  fetchImpl: typeof fetch = fetch,
  now = Date.now(),
): Promise<ApiResult> {
  const requestId = crypto.randomUUID()
  const contentLength = Number(metadata.contentLength ?? 0)
  if (Number.isFinite(contentLength) && contentLength > 12_000) return errorResult(413, 'request_too_large')
  if (!originAllowed(metadata.origin, environment)) return errorResult(403, 'origin_not_allowed')

  if (text(body.website, 100)) return { status: 202, body: { requestId, status: 'received' } }

  const email = text(body.email, 160).toLowerCase()
  const identifierType = text(body.identifierType, 20)
  const identifier = text(body.identifier, 120)
  const details = text(body.details, 500)
  const locale = body.locale === 'en' ? 'en' : 'es'
  const startedAt = typeof body.startedAt === 'number' ? body.startedAt : 0

  if (!validEmail(email) || !['alias', 'google'].includes(identifierType) || identifier.length < 3
    || body.consent !== true || !startedAt || now - startedAt < 1_500 || now - startedAt > 86_400_000) {
    return errorResult(400, 'invalid_request')
  }

  const apiKey = environment.RESEND_API_KEY?.trim()
  const from = environment.RESEND_FROM?.trim()
  const recipient = environment.PRIVACY_REQUEST_TO?.trim()
  if (!apiKey || !from || !recipient) return errorResult(503, 'service_unavailable')

  try {
    const response = await fetchImpl('https://api.resend.com/emails/batch', {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${apiKey}`,
        'Content-Type': 'application/json',
        'Idempotency-Key': requestId,
      },
      body: JSON.stringify(emails({ email, identifierType, identifier, details, locale }, requestId, environment)),
    })
    if (!response.ok) return errorResult(503, 'service_unavailable')
    return { status: 202, body: { requestId, status: 'received' } }
  } catch {
    return errorResult(503, 'service_unavailable')
  }
}
