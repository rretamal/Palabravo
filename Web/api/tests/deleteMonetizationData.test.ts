import { describe, it, expect } from 'vitest'
import { parseAnalyticsIds } from '../src/lib/deleteMonetizationData.js'

describe('analytics deletion identifiers', () => {
  it('accepts older clients without analytics identifiers', () => expect(parseAnalyticsIds({})).toEqual([]))
  it('deduplicates valid installation IDs', () => {
    const id = 'a'.repeat(32)
    expect(parseAnalyticsIds({ analyticsInstanceIds: [id, id] })).toEqual([id])
  })
  it('rejects arbitrary identifiers and unbounded lists', () => {
    expect(() => parseAnalyticsIds({ analyticsInstanceIds: ['someone@example.com'] })).toThrow()
    expect(() => parseAnalyticsIds({ analyticsInstanceIds: Array(33).fill('a'.repeat(32)) })).toThrow()
  })
})
