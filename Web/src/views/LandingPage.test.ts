import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'
import { playStoreUrl } from '../content'
import { routes, scrollBehavior } from '../router'
import LandingPage from './LandingPage.vue'

async function render(path: string) {
  const router = createRouter({ history: createMemoryHistory(), routes })
  await router.push(path)
  await router.isReady()
  return mount(LandingPage, { global: { plugins: [router] } })
}

describe('landing page', () => {
  it('renders the Spanish experience by default', async () => {
    const wrapper = await render('/')
    expect(wrapper.text()).toContain('Descubre conexiones.')
    expect(wrapper.text()).toContain('Gran Maestro')
    expect(wrapper.findAll(`a[href="${playStoreUrl}"]`).length).toBeGreaterThan(2)
    expect(wrapper.find('a[href="/#diario"]').exists()).toBe(true)
    expect(wrapper.find('a[href="/#como-jugar"]').exists()).toBe(true)
    expect(wrapper.find('a[href="/#progreso"]').exists()).toBe(true)
  })

  it('positions the English page as Spanish practice', async () => {
    const wrapper = await render('/en')
    expect(wrapper.text()).toContain('Build your Spanish.')
    expect(wrapper.text()).toContain('Vocabulary in context')
    expect(wrapper.find('a[href="/en#diario"]').exists()).toBe(true)
    expect(wrapper.find('a[href="/en#como-jugar"]').exists()).toBe(true)
    expect(wrapper.find('a[href="/en#progreso"]').exists()).toBe(true)
    expect(wrapper.find('html').exists()).toBe(false)
  })

  it('declares every localized legal route', () => {
    const paths = routes.map((route) => route.path)
    expect(paths).toEqual(expect.arrayContaining(['/privacidad', '/en/privacy', '/eliminar-datos', '/en/delete-data', '/terminos', '/en/terms']))
  })

  it('scrolls menu hashes to their target section', () => {
    expect(scrollBehavior({ hash: '#diario' } as never, {} as never, null)).toEqual({
      el: '#diario',
      behavior: 'smooth',
    })
    expect(scrollBehavior({ hash: '#inicio' } as never, {} as never, null)).toEqual({
      top: 0,
      behavior: 'smooth',
    })
    expect(scrollBehavior({ hash: '' } as never, {} as never, null)).toEqual({ top: 0 })
  })
})
