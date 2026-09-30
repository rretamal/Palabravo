import { describe, expect, it } from 'vitest'
import { bearerToken, playerAuthorization } from '../src/lib/http.js'

describe('player authorization through the Static Web Apps gateway', () => {
  it('preserves the player ticket when the gateway replaces Authorization', () => {
    const headers = new Headers({
      Authorization: 'Bearer azure-gateway-token',
      'X-Palabravo-Authorization': 'Bearer player-session',
    })
    expect(bearerToken(playerAuthorization(headers))).toBe('player-session')
  })

  it('supports direct requests using the legacy Authorization header', () => {
    expect(playerAuthorization(new Headers({ Authorization: 'Bearer player-session' })))
      .toBe('Bearer player-session')
  })

  it('does not fall back to the gateway token when the player header is empty or invalid', () => {
    for (const value of ['', 'not-a-bearer-token']) {
      const headers = new Headers({ Authorization: 'Bearer azure-gateway-token',
        'X-Palabravo-Authorization': value })
      expect(bearerToken(playerAuthorization(headers))).toBeNull()
    }
  })

  it('requires credentials', () => {
    expect(playerAuthorization(new Headers())).toBeNull()
  })
})
