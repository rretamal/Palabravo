import { beforeEach, describe, expect, it, vi } from 'vitest'
const table = vi.hoisted(() => ({ getEntity: vi.fn(), updateEntity: vi.fn() }))
vi.mock('@azure/data-tables', () => ({
  TableClient: { fromConnectionString: () => table }, odata: vi.fn(),
}))
import { AzurePathRankingRepository } from '../src/lib/pathRankingRepository.js'
import { AzureWeeklyRepository } from '../src/lib/weeklyRepository.js'

beforeEach(() => {
  vi.clearAllMocks()
  process.env.PURCHASE_STORAGE_CONNECTION = 'test'
  table.updateEntity.mockResolvedValue(undefined)
})
describe('ranking profile updates', () => {
  it('updates path name and flag without replacing a better score', async () => {
    const best = { playerId: 'p', name: 'Alias', score: 260, completed: 2,
      gold: 2, silver: 0, bronze: 0, updatedAt: 'old' }
    table.getEntity.mockResolvedValue({ score: 260, payload: JSON.stringify(best), etag: 'v1' })
    await new AzurePathRankingRepository().save({ ...best, name: '🇨🇱 Jugador', score: 130, completed: 1 })
    const entity = table.updateEntity.mock.calls[0][0]
    expect(JSON.parse(entity.payload)).toEqual({ ...best, name: '🇨🇱 Jugador' })
    expect(entity.score).toBe(260)
  })
  it('updates a weekly alias at the same score', async () => {
    const best = { weeklyId: 'week', playerId: 'p', name: 'Alias', score: 34299841,
      timeSeconds: 158, mistakes: 0, hints: 0, medal: 'gold', updatedAt: 'old' }
    table.getEntity.mockResolvedValue({ score: best.score, payload: JSON.stringify(best), etag: 'v1' })
    await new AzureWeeklyRepository().saveBest({ ...best, name: '🇨🇱 Jugador' })
    expect(JSON.parse(table.updateEntity.mock.calls[0][0].payload)).toEqual({ ...best, name: '🇨🇱 Jugador' })
  })
})
