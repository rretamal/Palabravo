import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

beforeEach(() => { vi.resetModules(); localStorage.clear(); document.head.innerHTML = ''; delete window.gtag; window.dataLayer = [] })
async function init(path = '/') {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/:pathMatch(.*)*', component: { template: '<div />' } }] })
  await router.push(path)
  const analytics = await import('./analytics')
  analytics.setupAnalytics(router)
  return { router, analytics }
}
describe('optional web analytics', () => {
  it('does not load Google before consent or after rejection', async () => {
    const { analytics } = await init()
    expect(document.querySelector('script')).toBeNull()
    analytics.setConsent('denied')
    expect(document.querySelector('script')).toBeNull()
    expect(window.dataLayer).toEqual([])
  })
  it('tracks sanitized SPA paths once and stops tracking after withdrawal', async () => {
    const { analytics, router } = await init('/challenge/private-code?email=private@example.com')
    analytics.setConsent('granted')
    expect(document.querySelectorAll('script')).toHaveLength(1)
    let events = window.dataLayer.map(item => Array.from(item as ArrayLike<unknown>)).filter(item => item[0] === 'event')
    expect(JSON.stringify(window.dataLayer)).not.toContain('private-code')
    expect(JSON.stringify(window.dataLayer)).not.toContain('private@example.com')
    expect(events[0]?.[2]).toMatchObject({ page_location: 'https://palabravo.app/challenge/[code]' })
    await router.push('/en')
    events = window.dataLayer.map(item => Array.from(item as ArrayLike<unknown>)).filter(item => item[0] === 'event')
    expect(events).toHaveLength(2)
    analytics.setConsent('denied')
    await router.push('/privacidad')
    expect(window.dataLayer.map(item => Array.from(item as ArrayLike<unknown>)).filter(item => item[0] === 'event')).toHaveLength(2)
  })
})
