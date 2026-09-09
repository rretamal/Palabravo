import { describe, expect, it, vi } from 'vitest'
import { createWeeklyInvite, getWeeklyRanking, resolveWeeklyInvite, submitWeekly,
  type WeeklyDependencies, type WeeklyInvite, type WeeklyResult } from '../src/lib/weekly.js'

const body = { weeklyId: 'chile-18-2026', name: 'Puma Ágil', score: 34299841,
  timeSeconds: 158, mistakes: 0, hints: 0, medal: 'gold' }
function setup() {
  const results = new Map<string, WeeklyResult>()
  const invites = new Map<string, WeeklyInvite>()
  const deps: WeeklyDependencies = {
    authenticate: vi.fn(async auth => auth === 'Bearer valid' ? 'player-1' : null),
    repository: {
      saveBest: vi.fn(async result => {
        const previous = results.get(result.playerId)
        if (!previous || previous.score < result.score) results.set(result.playerId, result)
      }),
      list: vi.fn(async () => [...results.values()]),
      createInvite: vi.fn(async invite => { if (invites.has(invite.token)) return false; invites.set(invite.token, invite); return true }),
      getInvite: vi.fn(async token => invites.get(token) ?? null),
    },
  }
  return { deps, results, invites }
}

describe('weekly events', () => {
  it('requires authentication and validates submitted scores', async () => {
    const { deps } = setup()
    expect((await submitWeekly(null, body, deps)).status).toBe(401)
    expect((await submitWeekly('Bearer valid', { ...body, score: -1 }, deps)).status).toBe(400)
    expect((await submitWeekly('Bearer valid', body, deps)).status).toBe(200)
  })

  it('orders ranking and reports the current percentile', async () => {
    const { deps, results } = setup()
    await submitWeekly('Bearer valid', body, deps)
    const own = [...results.values()][0]
    results.set('player-2', { ...body, playerId: 'player-2', name: 'Cóndor', score: body.score + 10, updatedAt: '' })
    const response = await getWeeklyRanking('Bearer valid', body.weeklyId, deps)
    expect(response.body.entries).toHaveLength(2)
    expect(response.body.currentPlayer).toMatchObject({ rank: 2, name: 'Puma Ágil' })
    expect(response.body.percentile).toBe(100)
    expect(own.playerId).not.toBe('player-1')
  })

  it('creates a six-character challenge and resolves it without authentication', async () => {
    const { deps } = setup()
    const created = await createWeeklyInvite('Bearer valid', { ...body, challenger: body.name }, deps)
    expect(created.status).toBe(201)
    const token = String(created.body.token)
    expect(token).toMatch(/^[A-Z0-9]{6}$/)
    expect((await resolveWeeklyInvite(token, deps)).body).toMatchObject({ weeklyId: body.weeklyId, challenger: body.name })
    expect((await resolveWeeklyInvite('bad', deps)).status).toBe(400)
  })
})
