import { AppStoreServerAPIClient, Environment, SignedDataVerifier } from '@apple/app-store-server-library'
import { GoogleAuth, OAuth2Client } from 'google-auth-library'
import { BUNDLE_ID, PRODUCT_ID, keyFor, type Proof, type StoreVerifier, type VerifiedPurchase } from './purchases.js'

const required = (name: string) => { const value = process.env[name]; if (!value) throw new Error(`${name} not configured`); return value }
const environment = (): 'production' | 'sandbox' => {
  const value = required('PURCHASE_ENVIRONMENT')
  if (value !== 'production' && value !== 'sandbox') throw new Error('Invalid purchase environment')
  return value
}

export function appleServices() {
  const env = environment() === 'production' ? Environment.PRODUCTION : Environment.SANDBOX
  // Root certificates come from deployment configuration, never from the incoming JWS.
  const roots = required('APPLE_ROOT_CERTIFICATES_BASE64').split(',').map(v => Buffer.from(v.trim(), 'base64'))
  const verifier = new SignedDataVerifier(roots, true, env, BUNDLE_ID,
    env === Environment.PRODUCTION ? Number(required('APPLE_APP_ID')) : undefined)
  const client = new AppStoreServerAPIClient(required('APPLE_SIGNING_KEY').replace(/\\n/g, '\n'), required('APPLE_KEY_ID'), required('APPLE_ISSUER_ID'), BUNDLE_ID, env)
  return { verifier, client }
}

export class OfficialStoreVerifier implements StoreVerifier {
  async verify(proof: Proof): Promise<VerifiedPurchase> {
    const checkedAt = Date.now()
    if (proof.platform === 'ios') {
      const { verifier, client } = appleServices()
      const submitted = await verifier.verifyAndDecodeTransaction(proof.proof)
      if (!submitted.transactionId || submitted.transactionId !== proof.transactionId || submitted.productId !== PRODUCT_ID) throw new Error('Invalid transaction')
      // Refresh even a valid signed transaction to account for a subsequent refund.
      const response = await client.getTransactionInfo(submitted.transactionId)
      const current = await verifier.verifyAndDecodeTransaction(response.signedTransactionInfo!)
      if (current.productId !== PRODUCT_ID || current.bundleId !== BUNDLE_ID || !current.originalTransactionId) throw new Error('Invalid product')
      const owned = !current.revocationDate
      return { key: keyFor(`${environment()}:${current.originalTransactionId}`), platform: 'ios',
        transactionId: current.transactionId!, proof: response.signedTransactionInfo!, owned,
        status: owned ? 'verified' : 'revoked', environment: environment(), checkedAt,
        purchasedAt: new Date(current.originalPurchaseDate!).toISOString(), currency: current.currency, valueMilli: current.price }
    }
    const auth = new GoogleAuth({ credentials: JSON.parse(required('GOOGLE_SERVICE_ACCOUNT_JSON')), scopes: ['https://www.googleapis.com/auth/androidpublisher'] })
    const client = await auth.getClient()
    const response = await client.request<{
      productLineItem?: { productId?: string; productOfferDetails?: { consumptionState?: string } }[]
      purchaseStateContext?: { purchaseState?: string }; testPurchaseContext?: unknown
      orderId?: string; purchaseCompletionTime?: string
    }>({ url: `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${BUNDLE_ID}/purchases/productsv2/tokens/${encodeURIComponent(proof.proof)}`, timeout: 10_000 })
    const data = response.data
    const item = data.productLineItem?.find(p => p.productId === PRODUCT_ID)
    if (!item || (data.testPurchaseContext ? 'sandbox' : 'production') !== environment()) throw new Error('Wrong product or environment')
    const state = data.purchaseStateContext?.purchaseState
    if (!['PURCHASED', 'PENDING', 'CANCELLED'].includes(state ?? '')) throw new Error('Unknown purchase state')
    const owned = state === 'PURCHASED' && item.productOfferDetails?.consumptionState !== 'CONSUMPTION_STATE_CONSUMED'
    return { key: keyFor(`${environment()}:${proof.proof}`), platform: 'android', transactionId: data.orderId ?? '', proof: proof.proof,
      owned, status: owned ? 'verified' : state === 'PENDING' ? 'pending' : 'revoked', environment: environment(), checkedAt,
      purchasedAt: data.purchaseCompletionTime }
  }
  async acknowledge(purchase: VerifiedPurchase): Promise<void> {
    if (!purchase.owned || purchase.platform !== 'android') return
    const auth = new GoogleAuth({ credentials: JSON.parse(required('GOOGLE_SERVICE_ACCOUNT_JSON')), scopes: ['https://www.googleapis.com/auth/androidpublisher'] })
    const client = await auth.getClient()
    const base = `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${BUNDLE_ID}/purchases/products/${PRODUCT_ID}/tokens/${encodeURIComponent(purchase.proof)}`
    const result = await client.request<{ acknowledgementState: number }>({ url: base, timeout: 10_000 })
    if (result.data.acknowledgementState === 1) return
    await client.request({ url: `${base}:acknowledge`, method: 'POST', data: {}, timeout: 10_000 })
  }
}

export async function verifyGoogleNotificationIdentity(authorization: string | null): Promise<void> {
  const match = /^Bearer ([^\s]+)$/.exec(authorization ?? '')
  if (!match) throw new Error('Missing notification identity')
  const ticket = await new OAuth2Client().verifyIdToken({ idToken: match[1]!, audience: required('GOOGLE_RTDN_AUDIENCE') })
  const payload = ticket.getPayload()
  if (!payload?.email_verified || payload.email !== required('GOOGLE_RTDN_SERVICE_ACCOUNT_EMAIL')) throw new Error('Invalid notification identity')
}
