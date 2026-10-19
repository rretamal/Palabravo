import { beforeEach, describe, expect, it, vi } from 'vitest'
const table = vi.hoisted(() => ({ getEntity: vi.fn(), updateEntity: vi.fn(), submitTransaction: vi.fn(), listEntities: vi.fn() }))
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
  it('atomically archives the guest while preserving the better linked score', async () => {
    const guest = { playerId: 'guest', name: 'Guest', score: 120, completed: 1,
      gold: 0, silver: 1, bronze: 0, updatedAt: 'old' }
    const linked = { ...guest, playerId: 'linked', name: 'Linked', score: 360, completed: 3,
      gold: 1, silver: 1, bronze: 1 }
    table.getEntity.mockImplementation(async (_partition, key) => ({
      score: key === 'guest' ? guest.score : linked.score,
      payload: JSON.stringify(key === 'guest' ? guest : linked), etag: key,
    }))
    table.submitTransaction.mockResolvedValue(undefined)
    await new AzurePathRankingRepository().merge('guest', 'linked')
    const operations = table.submitTransaction.mock.calls[0][0]
    expect(operations[0]).toEqual(['update', { partitionKey: 'path_v1', rowKey: 'guest', mergedInto: 'linked' }, 'Merge', { etag: 'guest' }])
    expect(JSON.parse(operations[1][1].payload)).toEqual(linked)
    expect(operations[1][3]).toEqual({ etag: 'linked' })
  })
  it('keeps a better guest score and does not revive an archived identity', async () => {
    const guest = { playerId: 'guest', name: 'Guest', score: 260, completed: 2,
      gold: 2, silver: 0, bronze: 0, updatedAt: 'old' }
    table.getEntity.mockImplementation(async (_partition, key) => {
      if (key === 'linked') throw { statusCode: 404 }
      return { score: 260, payload: JSON.stringify(guest), etag: 'v1' }
    })
    await new AzurePathRankingRepository().merge('guest', 'linked')
    const destination = table.submitTransaction.mock.calls[0][0][1]
    expect(destination[0]).toBe('create')
    expect(JSON.parse(destination[1].payload)).toEqual({ ...guest, playerId: 'linked' })
    table.getEntity.mockResolvedValue({ mergedInto: 'linked' })
    await new AzurePathRankingRepository().save(guest)
    expect(table.updateEntity).not.toHaveBeenCalled()
    table.listEntities.mockImplementation(async function* () {
      yield { payload: JSON.stringify(guest), mergedInto: 'linked' }
      yield { payload: JSON.stringify({ ...guest, playerId: 'linked' }) }
    })
    expect(await new AzurePathRankingRepository().list()).toEqual([{ ...guest, playerId: 'linked' }])
  })
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
