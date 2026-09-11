import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { routes } from '../router'
import WeeklyChallengePage from './WeeklyChallengePage.vue'
import WeeklyPage from './WeeklyPage.vue'

const weekly = { id: 'chile-18-2026', title: 'Especial Chile', subtitle: '¿Cuánto sabes?', flag: '🇨🇱',
  startsAt: '2026-09-07T00:00:00Z', endsAt: '2026-09-21T00:00:00Z',
  badge: { name: 'Chile 2026', imageUrl: '/badges/chile-2026.svg' } }
const catalog = { version: 1, challenges: [weekly] }
afterEach(() => vi.unstubAllGlobals())

describe('weekly pages', () => {
  it('renders the remotely supplied weekly event', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => catalog })))
    const wrapper = mount(WeeklyPage)
    await flushPromises()
    expect(wrapper.text()).toContain('Especial Chile')
    expect(wrapper.text()).toContain('16 palabras')
  })

  it('renders a valid challenge without exposing puzzle answers', async () => {
    const invite = { token: 'A7K29F', weeklyId: weekly.id, challenger: 'Puma Ágil', score: 34299841,
      timeSeconds: 158, mistakes: 0, medal: 'gold' }
    vi.stubGlobal('fetch', vi.fn(async (url: string) => ({ ok: true,
      json: async () => url.includes('/api/challenges/') ? invite : catalog })))
    const router = createRouter({ history: createMemoryHistory(), routes })
    await router.push('/challenge/A7K29F'); await router.isReady()
    const wrapper = mount(WeeklyChallengePage, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain('Puma Ágil te desafió')
    expect(wrapper.text()).toContain('2:38 · 0 errores')
    expect(wrapper.text()).not.toContain('Cueca')
  })
})
