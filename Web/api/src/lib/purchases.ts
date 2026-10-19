import { createHash } from 'node:crypto'
import { bearerToken, errorResult, type ApiResult } from './http.js'

export const PRODUCT_ID = 'palabravo_remove_ads'
export const BUNDLE_ID = 'com.palabravo.app'
export type Platform = 'android' | 'ios'
export interface Proof { platform: Platform; transactionId: string; proof: string }
export interface VerifiedPurchase {
  key: string; platform: Platform; transactionId: string; proof: string
  owned: boolean; status: 'pending' | 'verified' | 'revoked'; environment: 'production' | 'sandbox'
  checkedAt: number; purchasedAt?: string; currency?: string; valueMilli?: number
}
export interface PurchaseRepository {
  save(purchase: VerifiedPurchase): Promise<void>
  attach(player: string, purchase: VerifiedPurchase): Promise<void>
  list(player: string, platform: Platform): Promise<VerifiedPurchase[]>
}
export interface StoreVerifier {
  verify(proof: Proof): Promise<VerifiedPurchase>
  acknowledge(purchase: VerifiedPurchase): Promise<void>
}
export interface PurchaseDependencies {
  authenticate(authorization: string | null): Promise<string | null>
  repository: PurchaseRepository
  verifier: StoreVerifier
  reportFailure?(failure: PurchaseFailure): Promise<void>
}
export interface PurchaseFailure {
  operation: 'verify' | 'restore'; phase: string; reason: string; status?: number
}

async function reportFailure(deps: PurchaseDependencies, operation: PurchaseFailure['operation'], phase: string, error: unknown) {
  const value = error as { response?: { status?: unknown; data?: { error?: unknown } }; statusCode?: unknown; code?: unknown }
  const candidate = value?.response?.status ?? value?.statusCode
  const status = typeof candidate === 'number' && Number.isInteger(candidate) && candidate >= 100 && candidate <= 599 ? candidate : undefined
  const message = error instanceof Error ? error.message : ''
  const known: Record<string, string> = {
    'GOOGLE_SERVICE_ACCOUNT_JSON not configured': 'google_credentials_missing',
    'PURCHASE_ENVIRONMENT not configured': 'environment_missing',
    'Invalid purchase environment': 'environment_invalid',
    'Wrong product or environment': 'product_or_environment_mismatch',
    'Unknown purchase state': 'unknown_purchase_state',
  }
  const reason = known[message] ?? (error instanceof SyntaxError ? 'invalid_json'
    : value?.response?.data?.error === 'invalid_grant' ? 'invalid_grant'
    : status === 403 ? 'access_denied' : status === 401 ? 'unauthorized' : 'provider_or_storage_error')
  // Never log the exception: provider messages can contain URLs with purchase tokens.
  try { await deps.reportFailure?.({ operation, phase, reason, ...(status ? { status } : {}) }) } catch { /* Diagnostics cannot change delivery. */ }
}
export const keyFor = (value: string) => createHash('sha256').update(value).digest('hex')
export function parseProof(value: unknown): Proof | null {
  if (!value || typeof value !== 'object') return null
  const v = value as Record<string, unknown>
  if (v.platform !== 'android' && v.platform !== 'ios') return null
  if (typeof v.transactionId !== 'string' || v.transactionId.length > 256) return null
  if (typeof v.proof !== 'string' || v.proof.length < 1 || v.proof.length > 24_000) return null
  return { platform: v.platform, transactionId: v.transactionId, proof: v.proof }
}

export async function verifyPurchase(authorization: string | null, body: unknown, deps: PurchaseDependencies): Promise<ApiResult> {
  const proof = parseProof(body)
  if (!proof) return errorResult(400, 'invalid_purchase')
  let phase = 'authenticate'
  try {
    const player = await deps.authenticate(authorization)
    if (!player) return errorResult(401, 'invalid_session')
    phase = 'store_verify'
    const purchase = await deps.verifier.verify(proof)
    phase = 'save_purchase'
    await deps.repository.save(purchase)
    phase = 'attach_purchase'
    await deps.repository.attach(player, purchase)
    phase = 'acknowledge'
    if (purchase.owned) await deps.verifier.acknowledge(purchase)
    // A pending payment must never revoke a previously verified purchase.
    phase = 'list_entitlements'
    const all = await deps.repository.list(player, proof.platform)
    return { status: 200, body: { verified: purchase.status !== 'pending', owned: all.some(p => p.owned), source: 'store' } }
  } catch (error) { await reportFailure(deps, 'verify', phase, error); return errorResult(503, 'purchase_verification_unavailable') }
}

export async function getEntitlement(authorization: string | null, platform: string | null, deps: PurchaseDependencies): Promise<ApiResult> {
  if (platform !== 'android' && platform !== 'ios') return errorResult(400, 'invalid_platform')
  let phase = 'authenticate'
  try {
    const player = await deps.authenticate(authorization)
    if (!player) return errorResult(401, 'invalid_session')
    phase = 'list_entitlements'
    const purchases = await deps.repository.list(player, platform)
    // Reconcile with the store; a network failure is unknown, never a revocation.
    for (const purchase of purchases) {
      phase = 'store_verify'
      const current = await deps.verifier.verify(purchase)
      phase = 'save_purchase'
      await deps.repository.save(current)
      phase = 'acknowledge'
      if (current.owned) await deps.verifier.acknowledge(current)
    }
    phase = 'list_entitlements'
    const current = await deps.repository.list(player, platform)
    return { status: 200, body: { verified: true, owned: current.some(p => p.owned), source: 'reconciled' } }
  } catch (error) { await reportFailure(deps, 'restore', phase, error); return errorResult(503, 'entitlement_unavailable') }
}

export async function authenticatePlayer(authorization: string | null,
  environment: NodeJS.ProcessEnv = process.env, fetchImpl: typeof fetch = fetch): Promise<string | null> {
  const ticket = bearerToken(authorization)
  if (!ticket) return null
  const title = environment.PLAYFAB_TITLE_ID?.trim()
  if (!title || !/^[A-Za-z0-9]+$/.test(title)) throw new Error('PlayFab not configured')
  // An empty lookup returns only the ticket's authenticated account. Never
  // accept a player ID supplied by the caller as the authentication identity.
  const response = await fetchImpl(`https://${title}.playfabapi.com/Client/GetAccountInfo`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Authorization': ticket },
    body: '{}', signal: AbortSignal.timeout(10_000),
  })
  if (!response.ok) return null
  const envelope = await response.json() as {
    data?: { AccountInfo?: { PlayFabId?: string } }
  }
  const playerId = envelope.data?.AccountInfo?.PlayFabId
  return typeof playerId === 'string' && playerId.trim() ? playerId : null
}
