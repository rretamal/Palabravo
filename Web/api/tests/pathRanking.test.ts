import { describe, expect, it, vi } from 'vitest'
import { getPathRanking, submitPathResult, type PathRankingDependencies,
  type PathResult } from '../src/lib/pathRanking.js'

const body = { name: 'Puma Ágil', score: 370, completed: 3, gold: 2, silver: 0, bronze: 1 }
function setup() {
  const results = new Map<string, PathResult>()
  const deps: PathRankingDependencies = {
    authenticate: vi.fn(async auth => auth === 'Bearer valid' ? 'player-1' : auth === 'Bearer other' ? 'player-2' : null),
    repository: {
      save: vi.fn(async result => { results.set(result.playerId, result) }),
      list: vi.fn(async () => [...results.values()]),
    },
  }
  return { deps, results }
}

describe('path ranking', () => {
  it('validates totals and requires a player session', async () => {
    const { deps } = setup()
    expect((await submitPathResult(null, body, deps)).status).toBe(401)
    expect((await submitPathResult('Bearer valid', { ...body, score: 999 }, deps)).status).toBe(400)
    expect((await submitPathResult('Bearer valid', body, deps)).status).toBe(200)
  })

  it('orders players and reports points needed to advance', async () => {
    const { deps } = setup()
    await submitPathResult('Bearer valid', body, deps)
    await submitPathResult('Bearer other', { ...body, name: 'Cóndor', score: 380,
      gold: 2, silver: 1, bronze: 0 }, deps)
    const response = await getPathRanking('Bearer valid', deps)
    expect(response.body.entries).toHaveLength(2)
    expect(response.body.currentPlayer).toMatchObject({ rank: 2, name: body.name })
    expect(response.body.pointsToNext).toBe(11)
  })
})
