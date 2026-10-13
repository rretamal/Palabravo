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
        const existing = await this.table.getEntity<{ score: number; payload: string }>(entity.partitionKey, entity.rowKey)
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
    for await (const item of this.table.listEntities<{ payload: string }>({ queryOptions: {
      filter: `PartitionKey eq 'path_v1'`,
    } })) results.push(JSON.parse(item.payload) as PathResult)
    return results
  }
}
