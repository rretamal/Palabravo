import type { Router } from 'vue-router'

export const measurementId = 'G-XG9SFE6FXW'
export const consentKey = 'palabravo-web-analytics'
type Choice = 'granted' | 'denied'
let active = false
let router: Router
let lastPath = ''

declare global {
  interface Window { dataLayer: unknown[]; gtag?: (...args: unknown[]) => void }
}
export function readConsent(): Choice | null {
  try { const value = localStorage.getItem(consentKey); return value === 'granted' || value === 'denied' ? value : null } catch { return null }
}
function pageView() {
  const path = router.currentRoute.value.path
  // Do not transmit query strings, referral codes, form values, or user identifiers.
  const safePath = path.startsWith('/challenge/') ? '/challenge/[code]' : path.startsWith('/reto/') ? '/reto/[id]' : path
  if (!active || safePath === lastPath) return
  lastPath = safePath
  window.gtag?.('event', 'page_view', { page_location: `https://palabravo.app${safePath}`, page_title: String(router.currentRoute.value.meta.title ?? 'Palabravo'), page_referrer: '' })
}
export function setConsent(choice: Choice) {
  try { localStorage.setItem(consentKey, choice) } catch { /* Apply for this visit. */ }
  if (choice === 'denied') {
    if (active) window.gtag?.('consent', 'update', { analytics_storage: 'denied' })
    active = false
    lastPath = ''
    // Remove first-party Analytics cookies on withdrawal.
    for (const cookie of document.cookie.split(';')) {
      const name = cookie.trim().split('=')[0]
      if (!/^_ga(?:_|$)/.test(name)) continue
      for (const domain of ['', '; domain=palabravo.app', '; domain=.palabravo.app']) document.cookie = `${name}=; Max-Age=0; path=/${domain}`
    }
    return
  }
  if (active) return
  window.dataLayer ??= []
  window.gtag ??= function () { window.dataLayer.push(arguments) }
  window.gtag('consent', 'default', { analytics_storage: 'denied', ad_storage: 'denied', ad_user_data: 'denied', ad_personalization: 'denied' })
  window.gtag('consent', 'update', { analytics_storage: 'granted' })
  if (!document.getElementById('palabravo-google-analytics')) {
    const script = document.createElement('script')
    script.id = 'palabravo-google-analytics'; script.async = true
    script.src = `https://www.googletagmanager.com/gtag/js?id=${measurementId}`
    document.head.appendChild(script)
    window.gtag('js', new Date())
    window.gtag('config', measurementId, { send_page_view: false, allow_google_signals: false, allow_ad_personalization_signals: false, page_location: 'https://palabravo.app/', page_referrer: '' })
  }
  active = true
  pageView()
}
export function setupAnalytics(value: Router) {
  router = value
  router.afterEach(() => pageView())
  if (readConsent() === 'granted') setConsent('granted')
  document.addEventListener('click', event => {
    const anchor = (event.target as Element | null)?.closest('a')
    if (active && anchor?.href.startsWith('https://play.google.com/store/apps/details?id=com.palabravo.app')) window.gtag?.('event', 'download_click', { platform: 'android' })
  })
}
