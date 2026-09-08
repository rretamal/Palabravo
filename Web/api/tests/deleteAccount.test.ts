import { describe, expect, it, vi } from 'vitest'
import { processAccountDeletion } from '../src/lib/deleteAccount.js'

const environment = { PLAYFAB_TITLE_ID: '153ECF', PLAYFAB_SECRET_KEY: 'server-secret' }

describe('authenticated account deletion', () => {
  it('validates the ticket and queues deletion', async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: { PlayFabId: 'PF-123' } }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: { JobReceiptId: 'job-456' } }), { status: 200 }))

    const result = await processAccountDeletion('Bearer ticket-value', environment, fetchMock)

    expect(result).toEqual({ status: 202, body: { requestId: 'job-456', status: 'queued' } })
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(fetchMock.mock.calls[0][1]?.body).toBe(JSON.stringify({ Token: 'ticket-value', TokenType: 'SessionTicket' }))
    expect(fetchMock.mock.calls[1][1]?.body).toContain('PF-123')
  })

  it('rejects missing or malformed bearer tokens without calling PlayFab', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    expect((await processAccountDeletion(null, environment, fetchMock)).status).toBe(401)
    expect((await processAccountDeletion('Basic value', environment, fetchMock)).status).toBe(401)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('does not attempt deletion when the PlayFab session is invalid', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response('{}', { status: 400 }))
    const result = await processAccountDeletion('Bearer expired', environment, fetchMock)
    expect(result).toEqual({ status: 401, body: { error: 'invalid_session' } })
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('returns a retryable error when PlayFab deletion fails', async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: { PlayFabId: 'PF-123' } }), { status: 200 }))
      .mockResolvedValueOnce(new Response('{}', { status: 500 }))
    const result = await processAccountDeletion('Bearer ticket', environment, fetchMock)
    expect(result).toEqual({ status: 502, body: { error: 'deletion_not_accepted' } })
  })
})
