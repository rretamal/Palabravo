import { TableClient, odata } from '@azure/data-tables'
import { createHash } from 'node:crypto'
import type { WeeklyInvite, WeeklyRepository, WeeklyResult } from './weekly.js'

export class AzureWeeklyRepository implements WeeklyRepository {
  private readonly table: TableClient
  constructor() {
    const connection = process.env.PURCHASE_STORAGE_CONNECTION
    if (!connection) throw new Error('Weekly storage not configured')
    this.table = TableClient.fromConnectionString(connection, 'PalabravoWeekly')
  }
  async saveBest(result: WeeklyResult): Promise<void> {
    const entity = { partitionKey: `weekly_${result.weeklyId}`,
      rowKey: result.playerId, score: result.score,
      payload: JSON.stringify(result) }
    for (let attempt = 0; attempt < 2; attempt++) {
      try {
        const existing = await this.table.getEntity<{ score: number }>(entity.partitionKey, entity.rowKey)
        if (existing.score >= result.score) return
        await this.table.updateEntity(entity, 'Replace', { etag: existing.etag }); return
      } catch (error) {
        const status = (error as { statusCode?: number }).statusCode
        if (status === 404) {
          try { await this.table.createEntity(entity); return }
          catch (createError) { if ((createError as { statusCode?: number }).statusCode !== 409) throw createError }
        } else if (status !== 412) throw error
      }
    }
    throw new Error('Concurrent weekly update; retry required')
  }
  async list(weeklyId: string): Promise<WeeklyResult[]> {
    const results: WeeklyResult[] = []
    for await (const item of this.table.listEntities<{ payload: string }>({ queryOptions: {
      filter: odata`PartitionKey eq ${`weekly_${weeklyId}`}`,
    } })) results.push(JSON.parse(item.payload) as WeeklyResult)
    return results
  }
  async createInvite(invite: WeeklyInvite): Promise<boolean> {
    try {
      await this.table.createEntity({ partitionKey: 'challenges', rowKey: invite.token,
        payload: JSON.stringify(invite), ownerKey: invite.ownerKey,
        expiresAt: new Date(Date.now() + 31 * 86_400_000).toISOString() })
      return true
    } catch (error) {
      if ((error as { statusCode?: number }).statusCode === 409) return false
      throw error
    }
  }
  async getInvite(token: string): Promise<WeeklyInvite | null> {
    try {
      const entity = await this.table.getEntity<{ payload: string; expiresAt: string }>('challenges', token)
      if (new Date(entity.expiresAt) <= new Date()) return null
      return JSON.parse(entity.payload) as WeeklyInvite
    } catch (error) {
      if ((error as { statusCode?: number }).statusCode === 404) return null
      throw error
    }
  }
  async removePlayer(playerId: string): Promise<void> {
    const key = createHash('sha256').update(playerId).digest('hex')
    for await (const item of this.table.listEntities({ queryOptions: { filter: odata`RowKey eq ${key}` } }))
      await this.table.deleteEntity(item.partitionKey!, item.rowKey!)
    for await (const item of this.table.listEntities({ queryOptions: {
      filter: odata`PartitionKey eq ${'challenges'} and ownerKey eq ${key}`,
    } })) await this.table.deleteEntity(item.partitionKey!, item.rowKey!)
  }
}
