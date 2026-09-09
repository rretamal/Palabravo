import { GoogleAuth } from 'google-auth-library'
import { AzurePurchaseRepository } from './purchaseRepository.js'
import { AzureWeeklyRepository } from './weeklyRepository.js'

export function parseAnalyticsIds(body: unknown): string[] {
  if (!body || typeof body !== 'object') return []
  const ids = (body as { analyticsInstanceIds?: unknown }).analyticsInstanceIds
  if (ids === undefined) return []
  if (!Array.isArray(ids) || ids.length > 32 || ids.some(id => typeof id !== 'string' || !/^[a-fA-F0-9]{32}$/.test(id)))
    throw new Error('Invalid analytics identifiers')
  return [...new Set(ids)]
}

export async function deleteMonetizationData(playerId: string, analyticsIds: string[]): Promise<void> {
  if (analyticsIds.length > 0) {
    const propertyId = process.env.ANALYTICS_PROPERTY_ID
    const credentials = process.env.ANALYTICS_SERVICE_ACCOUNT_JSON
    if (!propertyId || !/^\d+$/.test(propertyId) || !credentials) throw new Error('Analytics deletion not configured')
    const auth = new GoogleAuth({ credentials: JSON.parse(credentials), scopes: ['https://www.googleapis.com/auth/analytics.edit'] })
    const client = await auth.getClient()
    for (const id of analyticsIds)
      await client.request({ url: `https://analyticsadmin.googleapis.com/v1alpha/properties/${propertyId}:submitUserDeletion`, method: 'POST', data: { appInstanceId: id }, timeout: 10_000 })
  }
  if (process.env.PURCHASE_ENVIRONMENT || process.env.PURCHASE_STORAGE_CONNECTION)
  {
    await new AzurePurchaseRepository().removePlayer(playerId)
    await new AzureWeeklyRepository().removePlayer(playerId)
  }
}
