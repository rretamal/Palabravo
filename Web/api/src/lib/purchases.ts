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
  try {
    const player = await deps.authenticate(authorization)
    if (!player) return errorResult(401, 'invalid_session')
    const purchase = await deps.verifier.verify(proof)
    await deps.repository.save(purchase)
    await deps.repository.attach(player, purchase)
    if (purchase.owned) await deps.verifier.acknowledge(purchase)
    // A pending payment must never revoke a previously verified purchase.
    const all = await deps.repository.list(player, proof.platform)
    return { status: 200, body: { verified: purchase.status !== 'pending', owned: all.some(p => p.owned), source: 'store' } }
  } catch { return errorResult(503, 'purchase_verification_unavailable') }
}

export async function getEntitlement(authorization: string | null, platform: string | null, deps: PurchaseDependencies): Promise<ApiResult> {
  if (platform !== 'android' && platform !== 'ios') return errorResult(400, 'invalid_platform')
  try {
    const player = await deps.authenticate(authorization)
    if (!player) return errorResult(401, 'invalid_session')
    const purchases = await deps.repository.list(player, platform)
    // Reconcile with the store; a network failure is unknown, never a revocation.
    for (const purchase of purchases) {
      const current = await deps.verifier.verify(purchase)
      await deps.repository.save(current)
      if (current.owned) await deps.verifier.acknowledge(current)
    }
    const current = await deps.repository.list(player, platform)
    return { status: 200, body: { verified: true, owned: current.some(p => p.owned), source: 'reconciled' } }
  } catch { return errorResult(503, 'entitlement_unavailable') }
}

export async function authenticatePlayer(authorization: string | null,
  environment: NodeJS.ProcessEnv = process.env, fetchImpl: typeof fetch = fetch): Promise<string | null> {
  const ticket = bearerToken(authorization)
  if (!ticket) return null
  const title = environment.PLAYFAB_TITLE_ID
  const secret = environment.PLAYFAB_SECRET_KEY
  if (!title || !/^[A-Za-z0-9]+$/.test(title) || !secret) throw new Error('PlayFab not configured')
  const response = await fetchImpl(`https://${title}.playfabapi.com/Server/AuthenticateSessionTicket`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-SecretKey': secret },
    body: JSON.stringify({ SessionTicket: ticket }), signal: AbortSignal.timeout(10_000),
  })
  if (!response.ok) return null
  const envelope = await response.json() as {
    data?: { IsSessionTicketExpired?: boolean; UserInfo?: { PlayFabId?: string } }
  }
  return envelope.data?.IsSessionTicketExpired === false
    ? envelope.data.UserInfo?.PlayFabId ?? null
    : null
}
