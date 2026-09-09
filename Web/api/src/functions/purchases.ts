import { app, type HttpRequest } from '@azure/functions'
import { authenticatePlayer, BUNDLE_ID, getEntitlement, verifyPurchase, type PurchaseDependencies } from '../lib/purchases.js'
import { AzurePurchaseRepository } from '../lib/purchaseRepository.js'
import { OfficialStoreVerifier, appleServices, verifyGoogleNotificationIdentity } from '../lib/storeVerifier.js'
import { errorResult, safeJsonResponse } from '../lib/http.js'

const dependencies = (): PurchaseDependencies => ({ authenticate: authenticatePlayer, repository: new AzurePurchaseRepository(), verifier: new OfficialStoreVerifier() })
async function json(request: HttpRequest): Promise<unknown> {
  const text = await request.text()
  if (text.length > 40_000) throw new Error('Body too large')
  return JSON.parse(text)
}
app.http('purchase-verify', { methods: ['POST'], authLevel: 'anonymous', route: 'purchases/verify', handler: async request => {
  let body: unknown
  try { body = await json(request) } catch { return safeJsonResponse(errorResult(400, 'invalid_body')) }
  try { return safeJsonResponse(await verifyPurchase(request.headers.get('authorization'), body, dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('purchase-entitlement', { methods: ['GET'], authLevel: 'anonymous', route: 'purchases/entitlement', handler: async request => {
  try { return safeJsonResponse(await getEntitlement(request.headers.get('authorization'), request.query.get('platform'), dependencies())) }
  catch { return safeJsonResponse(errorResult(503, 'service_unavailable')) }
} })
app.http('purchase-apple-notification', { methods: ['POST'], authLevel: 'anonymous', route: 'purchases/notifications/apple', handler: async request => {
  try {
    const body = await json(request) as { signedPayload: string }
    const { verifier } = appleServices()
    const notification = await verifier.verifyAndDecodeNotification(body.signedPayload)
    if (notification.data?.signedTransactionInfo) {
      const transaction = await verifier.verifyAndDecodeTransaction(notification.data.signedTransactionInfo)
      const deps = dependencies()
      const current = await deps.verifier.verify({ platform: 'ios', transactionId: transaction.transactionId!, proof: notification.data.signedTransactionInfo })
      await deps.repository.save(current)
    }
    return { status: 200 }
  } catch { return { status: 503 } } // Retry transient verification/storage errors; never acknowledge lost updates.
} })
app.http('purchase-google-notification', { methods: ['POST'], authLevel: 'anonymous', route: 'purchases/notifications/google', handler: async request => {
  try { await verifyGoogleNotificationIdentity(request.headers.get('authorization')) }
  catch { return { status: 401 } }
  try {
    const body = await json(request) as { message: { data: string } }
    const notification = JSON.parse(Buffer.from(body.message.data, 'base64').toString()) as {
      packageName: string; oneTimeProductNotification?: { purchaseToken: string }
      voidedPurchaseNotification?: { purchaseToken: string }; testNotification?: unknown
    }
    if (notification.packageName !== BUNDLE_ID) return { status: 400 }
    const token = notification.oneTimeProductNotification?.purchaseToken ?? notification.voidedPurchaseNotification?.purchaseToken
    if (token) {
      const deps = dependencies()
      const current = await deps.verifier.verify({ platform: 'android', transactionId: '', proof: token })
      await deps.repository.save(current)
      if (current.owned) await deps.verifier.acknowledge(current)
    }
    return { status: 200 }
  } catch { return { status: 503 } }
} })
