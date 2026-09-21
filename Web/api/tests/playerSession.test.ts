import { describe, expect, it, vi } from 'vitest'
import { createPlayerSession } from '../src/lib/playerSession.js'

const environment = { PLAYFAB_TITLE_ID: '153ECF', PLAYFAB_SECRET_KEY: 'secret' }

describe('player session', () => {
  it('creates anonymous accounts through the protected server API', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: {
      SessionTicket: 'ticket',
      EntityToken: { EntityToken: 'entity-token', TokenExpiration: '2026-09-20T00:00:00Z',
        Entity: { Id: 'entity-id', Type: 'title_player_account' } },
    } }), { status: 200 }))

    const result = await createPlayerSession({ customId: '0123456789abcdef0123456789abcdef' },
      environment, fetchMock)

    expect(result.status).toBe(200)
    expect(fetchMock).toHaveBeenCalledWith('https://153ECF.playfabapi.com/Server/LoginWithCustomID',
      expect.objectContaining({ method: 'POST', headers: expect.objectContaining({ 'X-SecretKey': 'secret' }) }))
    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({
      CustomId: '0123456789abcdef0123456789abcdef', CreateAccount: true,
    })
    expect(result.body).toMatchObject({ sessionTicket: 'ticket',
      entityToken: { entityToken: 'entity-token', entity: { id: 'entity-id' } } })
  })

  it('rejects malformed identifiers without contacting PlayFab', async () => {
    const fetchMock = vi.fn()
    const result = await createPlayerSession({ customId: 'predictable' }, environment, fetchMock)
    expect(result).toEqual({ status: 400, body: { error: 'invalid_custom_id' } })
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('fails safely when server credentials are missing', async () => {
    const result = await createPlayerSession({ customId: '0123456789abcdef0123456789abcdef' }, {})
    expect(result).toEqual({ status: 503, body: { error: 'playfab_not_configured' } })
  })
})
