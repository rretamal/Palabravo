import { describe, expect, it, vi } from 'vitest'
import { processDeletionRequest } from '../src/lib/deletionRequest.js'

const now = Date.now()
const environment = {
  RESEND_API_KEY: 'resend-secret',
  RESEND_FROM: 'Palabravo <hello@palabravo.app>',
  PRIVACY_REQUEST_TO: 'hello@palabravo.app',
  PUBLIC_SITE_URL: 'https://palabravo.app',
}
const validBody = {
  email: 'player@example.com', identifierType: 'alias', identifier: 'Ágil Búho 123', details: '', locale: 'es', website: '', consent: true, startedAt: now - 2_000,
}
const metadata = { origin: 'https://palabravo.app', contentLength: '260' }

describe('manual deletion requests', () => {
  it('sends one idempotent batch and returns a reference', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify({ data: [{ id: 'one' }, { id: 'two' }] }), { status: 200 }))
    const result = await processDeletionRequest(validBody, metadata, environment, fetchMock, now)
    expect(result.status).toBe(202)
    expect(result.body.status).toBe('received')
    expect(String(result.body.requestId)).toMatch(/^[0-9a-f-]{36}$/)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0][0]).toBe('https://api.resend.com/emails/batch')
    expect((fetchMock.mock.calls[0][1]?.headers as Record<string, string>)['Idempotency-Key']).toBe(result.body.requestId)
  })

  it('rejects invalid fields and origins before sending mail', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    expect((await processDeletionRequest({ ...validBody, email: 'bad' }, metadata, environment, fetchMock, now)).status).toBe(400)
    expect((await processDeletionRequest(validBody, { ...metadata, origin: 'https://attacker.example' }, environment, fetchMock, now)).status).toBe(403)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('accepts the honeypot silently without sending mail', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    const result = await processDeletionRequest({ ...validBody, website: 'spam' }, metadata, environment, fetchMock, now)
    expect(result.status).toBe(202)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('returns a generic retryable error when Resend fails', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response('{}', { status: 429 }))
    const result = await processDeletionRequest(validBody, metadata, environment, fetchMock, now)
    expect(result).toEqual({ status: 503, body: { error: 'service_unavailable' } })
  })
})
