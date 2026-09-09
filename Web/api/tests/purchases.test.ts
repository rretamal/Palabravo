import { describe, expect, it, vi } from 'vitest'
import { getEntitlement, parseProof, verifyPurchase, type PurchaseDependencies, type VerifiedPurchase } from '../src/lib/purchases.js'

const proof = { platform: 'android', transactionId: 'order', proof: 'purchase-token' }
function setup() {
  const records = new Map<string, VerifiedPurchase>()
  const grants = new Map<string, Set<string>>()
  const current: VerifiedPurchase = { ...proof, platform: 'android', key: 'canonical', owned: true, status: 'verified', environment: 'sandbox', checkedAt: 1 }
  const calls: string[] = []
  const deps: PurchaseDependencies = {
    authenticate: vi.fn(async auth => auth === 'Bearer valid' ? 'player' : null),
    verifier: { verify: vi.fn(async () => ({ ...current })), acknowledge: vi.fn(async () => { calls.push('acknowledge') }) },
    repository: {
      save: vi.fn(async purchase => { calls.push('save'); records.set(purchase.key, purchase) }),
      attach: vi.fn(async (player, purchase) => { const keys = grants.get(player) ?? new Set<string>(); keys.add(purchase.key); grants.set(player, keys) }),
      list: vi.fn(async (player, platform) => [...(grants.get(player) ?? [])].map(key => records.get(key)!).filter(p => p.platform === platform)),
    },
  }
  return { deps, records, current, calls }
}
describe('purchase verification', () => {
  it('rejects unauthenticated purchases before contacting a store', async () => {
    const { deps } = setup()
    expect((await verifyPurchase(null, proof, deps)).status).toBe(401)
    expect(deps.verifier.verify).not.toHaveBeenCalled()
  })
  it('rejects invalid platform, oversized proof and non-string transaction IDs', () => {
    expect(parseProof({ ...proof, platform: 'web' })).toBeNull()
    expect(parseProof({ ...proof, proof: 'x'.repeat(24_001) })).toBeNull()
    expect(parseProof({ ...proof, transactionId: 12 })).toBeNull()
  })
  it('stores delivery before acknowledgement and restores without duplicating the sale', async () => {
    const { deps, records, calls } = setup()
    expect((await verifyPurchase('Bearer valid', proof, deps)).body.owned).toBe(true)
    await verifyPurchase('Bearer valid', proof, deps)
    expect(records.size).toBe(1)
    expect(calls.slice(0, 2)).toEqual(['save', 'acknowledge'])
  })
  it('never acknowledges pending purchases or treats them as verified revenue', async () => {
    const { deps, current } = setup()
    current.owned = false; current.status = 'pending'
    expect((await verifyPurchase('Bearer valid', proof, deps)).body).toMatchObject({ verified: false, owned: false })
    expect(deps.verifier.acknowledge).not.toHaveBeenCalled()
  })
  it('does not grant on forged proof or provider failure', async () => {
    const { deps } = setup()
    vi.mocked(deps.verifier.verify).mockRejectedValue(new Error('signature invalid'))
    expect((await verifyPurchase('Bearer valid', proof, deps)).status).toBe(503)
    expect(deps.repository.save).not.toHaveBeenCalled()
  })
  it('retains unknown state during outage and reconciles revocation later', async () => {
    const { deps, current } = setup()
    await verifyPurchase('Bearer valid', proof, deps)
    vi.mocked(deps.verifier.verify).mockRejectedValueOnce(new Error('offline'))
    const offline = await getEntitlement('Bearer valid', 'android', deps)
    expect(offline.status).toBe(503)
    expect(offline.body).not.toHaveProperty('owned')
    current.owned = false; current.status = 'revoked'; current.checkedAt++
    expect((await getEntitlement('Bearer valid', 'android', deps)).body.owned).toBe(false)
  })
  it('keeps platform entitlements separate', async () => {
    const { deps } = setup()
    await verifyPurchase('Bearer valid', proof, deps)
    expect((await getEntitlement('Bearer valid', 'ios', deps)).body.owned).toBe(false)
  })
  it('does not acknowledge when durable storage fails', async () => {
    const { deps } = setup()
    vi.mocked(deps.repository.save).mockRejectedValue(new Error('storage down'))
    expect((await verifyPurchase('Bearer valid', proof, deps)).status).toBe(503)
    expect(deps.verifier.acknowledge).not.toHaveBeenCalled()
  })
})
