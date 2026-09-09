import { TableClient, odata } from '@azure/data-tables'
import { keyFor, type Platform, type PurchaseRepository, type VerifiedPurchase } from './purchases.js'

export class AzurePurchaseRepository implements PurchaseRepository {
  private readonly table: TableClient
  constructor() {
    const connection = process.env.PURCHASE_STORAGE_CONNECTION
    if (!connection) throw new Error('Purchase storage not configured')
    this.table = TableClient.fromConnectionString(connection, 'PalabravoPurchases')
  }
  async save(purchase: VerifiedPurchase): Promise<void> {
    // The canonical transaction is shared by all restores. ETags prevent a delayed
    // verification from overwriting a newer revocation, including concurrent webhooks.
    const entity = { partitionKey: purchase.platform, rowKey: purchase.key, payload: JSON.stringify(purchase), checkedAt: purchase.checkedAt }
    for (let attempt = 0; attempt < 2; attempt++) {
      try {
        const existing = await this.table.getEntity<{ checkedAt: number }>(entity.partitionKey, entity.rowKey)
        if (existing.checkedAt >= purchase.checkedAt) return
        await this.table.updateEntity(entity, 'Replace', { etag: existing.etag })
        return
      } catch (error) {
        const code = (error as { statusCode?: number }).statusCode
        if (code === 404) {
          try { await this.table.createEntity(entity); return }
          catch (createError) { if ((createError as { statusCode?: number }).statusCode !== 409) throw createError }
        } else if (code !== 412) throw error
      }
    }
    throw new Error('Concurrent purchase update; retry required')
  }
  async attach(player: string, purchase: VerifiedPurchase): Promise<void> {
    const partitionKey = `player_${keyFor(player)}`
    try { await this.table.createEntity({ partitionKey, rowKey: '_state', deleted: false }) }
    catch (error) { if ((error as { statusCode?: number }).statusCode !== 409) throw error }
    const state = await this.table.getEntity<{ deleted: boolean }>(partitionKey, '_state')
    if (state.deleted) throw new Error('Account deleted')
    await this.table.submitTransaction([
      ['update', { partitionKey, rowKey: '_state', deleted: false }, 'Replace', { etag: state.etag }],
      ['upsert', { partitionKey, rowKey: `${purchase.platform}_${purchase.key}`, platform: purchase.platform, transactionKey: purchase.key }, 'Replace'],
    ])
  }
  async removePlayer(player: string): Promise<void> {
    const partitionKey = `player_${keyFor(player)}`
    // Tombstone and attach's conditional transaction prevent an in-flight
    // verification from recreating references after deletion.
    await this.table.upsertEntity({ partitionKey, rowKey: '_state', deleted: true }, 'Replace')
    for await (const entry of this.table.listEntities({ queryOptions: { filter: odata`PartitionKey eq ${partitionKey}` } })) {
      if (entry.rowKey === '_state') continue
      try { await this.table.deleteEntity(partitionKey, entry.rowKey!) }
      catch (error) { if ((error as { statusCode?: number }).statusCode !== 404) throw error }
    }
  }
  async list(player: string, platform: Platform): Promise<VerifiedPurchase[]> {
    const entries = this.table.listEntities<{ transactionKey: string }>({ queryOptions: {
      filter: odata`PartitionKey eq ${`player_${keyFor(player)}`} and platform eq ${platform}`,
    } })
    const purchases: VerifiedPurchase[] = []
    for await (const reference of entries) {
      const record = await this.table.getEntity<{ payload: string }>(platform, reference.transactionKey)
      purchases.push(JSON.parse(record.payload) as VerifiedPurchase)
    }
    return purchases
  }
}
