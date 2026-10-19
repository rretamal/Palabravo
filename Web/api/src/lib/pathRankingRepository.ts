import { TableClient } from '@azure/data-tables'
import type { PathRankingRepository, PathResult } from './pathRanking.js'

export class AzurePathRankingRepository implements PathRankingRepository {
  private readonly table: TableClient
  constructor() {
    const connection = process.env.PURCHASE_STORAGE_CONNECTION
    if (!connection) throw new Error('Path ranking storage not configured')
    this.table = TableClient.fromConnectionString(connection, 'PalabravoWeekly')
  }
  async save(result: PathResult): Promise<void> {
    const entity = { partitionKey: 'path_v1', rowKey: result.playerId,
      score: result.score, payload: JSON.stringify(result) }
    for (let attempt = 0; attempt < 2; attempt++) {
      try {
        const existing = await this.table.getEntity<{ score: number; payload: string; mergedInto?: string }>(entity.partitionKey, entity.rowKey)
        if (existing.mergedInto) return
        if (existing.score >= result.score) {
          const best = JSON.parse(existing.payload) as PathResult
          if (best.name === result.name) return
          entity.score = existing.score
          entity.payload = JSON.stringify({ ...best, name: result.name })
        }
        await this.table.updateEntity(entity, 'Replace', { etag: existing.etag }); return
      } catch (error) {
        const status = (error as { statusCode?: number }).statusCode
        if (status === 404) {
          try { await this.table.createEntity(entity); return }
          catch (createError) { if ((createError as { statusCode?: number }).statusCode !== 409) throw createError }
        } else if (status !== 412) throw error
      }
    }
    throw new Error('Concurrent path update; retry required')
  }
  async list(): Promise<PathResult[]> {
    const results: PathResult[] = []
    for await (const item of this.table.listEntities<{ payload: string; mergedInto?: string }>({ queryOptions: {
      filter: `PartitionKey eq 'path_v1'`,
    } })) if (!item.mergedInto) results.push(JSON.parse(item.payload) as PathResult)
    return results
  }

  async merge(source: string, target: string): Promise<void> {
    if (source === target) return
    const partitionKey = 'path_v1'
    type Stored = { score: number; payload: string; mergedInto?: string }
    const read = async (key: string) => {
      try { return await this.table.getEntity<Stored>(partitionKey, key) }
      catch (error) { if ((error as { statusCode?: number }).statusCode === 404) return null; throw error }
    }
    for (let attempt = 0; attempt < 3; attempt++) {
      const from = await read(source)
      if (!from) return
      if (from.mergedInto === target) return
      if (from.mergedInto) throw new Error('Source ranking already merged')
      const to = await read(target)
      if (to?.mergedInto) throw new Error('Target ranking already merged')
      const oldResult = JSON.parse(from.payload) as PathResult
      const current = to ? JSON.parse(to.payload) as PathResult : null
      const best = current && current.score >= oldResult.score ? current : oldResult
      const result = { ...best, playerId: target, name: current?.name ?? best.name }
      const destination = { partitionKey, rowKey: target, score: result.score, payload: JSON.stringify(result) }
      try {
        // Preserve the source as a recoverable archive and update both rows atomically.
        await this.table.submitTransaction([
          ['update', { partitionKey, rowKey: source, mergedInto: target }, 'Merge', { etag: from.etag }],
          to ? ['update', destination, 'Replace', { etag: to.etag }] : ['create', destination],
        ])
        return
      } catch (error) {
        if (![409, 412].includes((error as { statusCode?: number }).statusCode ?? 0)) throw error
      }
    }
    throw new Error('Concurrent ranking migration; retry required')
  }
}
