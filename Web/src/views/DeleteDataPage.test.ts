import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { routes } from '../router'
import DeleteDataPage from './DeleteDataPage.vue'

afterEach(() => vi.unstubAllGlobals())

describe('delete data page', () => {
  it('submits only the expected request fields and shows the reference', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ requestId: 'request-123', status: 'received' }), {
      status: 202,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)
    const router = createRouter({ history: createMemoryHistory(), routes })
    await router.push('/eliminar-datos')
    await router.isReady()
    const wrapper = mount(DeleteDataPage, { global: { plugins: [router] } })

    expect(wrapper.get('.language-link').attributes('href')).toBe('/en/delete-data')

    await wrapper.get('input[type="email"]').setValue('player@example.com')
    const inputs = wrapper.findAll('input')
    await inputs[1].setValue('Ágil Búho 123')
    await wrapper.get('input[type="checkbox"]').setValue(true)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const options = fetchMock.mock.calls[0][1] as RequestInit
    expect(JSON.parse(String(options.body))).toMatchObject({ email: 'player@example.com', identifier: 'Ágil Búho 123', locale: 'es' })
    expect(wrapper.text()).toContain('request-123')
  })

  it('renders English instructions without asking for credentials', async () => {
    const router = createRouter({ history: createMemoryHistory(), routes })
    await router.push('/en/delete-data')
    await router.isReady()
    const wrapper = mount(DeleteDataPage, { global: { plugins: [router] } })
    expect(wrapper.text()).toContain('Delete account and data')
    expect(wrapper.text()).toContain('Do not include passwords or codes')
    expect(wrapper.find('input[type="password"]').exists()).toBe(false)
  })
})
