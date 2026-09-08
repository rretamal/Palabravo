import type { RouteRecordRaw, RouterScrollBehavior } from 'vue-router'
import LandingPage from './views/LandingPage.vue'
import LegalPage from './views/LegalPage.vue'
import DeleteDataPage from './views/DeleteDataPage.vue'

const meta = (
  locale: 'es' | 'en',
  title: string,
  description: string,
  esPath: string,
  enPath: string,
) => ({ locale, title, description, esPath, enPath })

export const scrollBehavior: RouterScrollBehavior = (to, _from, savedPosition) => {
  if (savedPosition) return savedPosition
  if (to.hash === '#inicio') return { top: 0, behavior: 'smooth' }
  if (to.hash) return { el: to.hash, behavior: 'smooth' }
  return { top: 0 }
}

export const routes: RouteRecordRaw[] = [
  {
    path: '/', component: LandingPage,
    meta: meta('es', 'Palabravo — Conecta palabras en español', 'Encuentra conexiones entre palabras en español, completa retos diarios y avanza por seis rangos.', '/', '/en'),
  },
  {
    path: '/en', component: LandingPage,
    meta: meta('en', 'Palabravo — Practice Spanish through word connections', 'Practice Spanish vocabulary by finding word connections, completing daily challenges, and advancing through six ranks.', '/', '/en'),
  },
  {
    path: '/privacidad', component: LegalPage, props: { kind: 'privacy' },
    meta: meta('es', 'Privacidad — Palabravo', 'Política de privacidad de Palabravo.', '/privacidad', '/en/privacy'),
  },
  {
    path: '/en/privacy', component: LegalPage, props: { kind: 'privacy' },
    meta: meta('en', 'Privacy — Palabravo', 'Palabravo privacy policy.', '/privacidad', '/en/privacy'),
  },
  {
    path: '/terminos', component: LegalPage, props: { kind: 'terms' },
    meta: meta('es', 'Términos de uso — Palabravo', 'Términos de uso de Palabravo.', '/terminos', '/en/terms'),
  },
  {
    path: '/en/terms', component: LegalPage, props: { kind: 'terms' },
    meta: meta('en', 'Terms of use — Palabravo', 'Palabravo terms of use.', '/terminos', '/en/terms'),
  },
  {
    path: '/eliminar-datos', component: DeleteDataPage,
    meta: meta('es', 'Eliminar cuenta y datos — Palabravo', 'Solicita la eliminación de tu cuenta de Palabravo y sus datos asociados.', '/eliminar-datos', '/en/delete-data'),
  },
  {
    path: '/en/delete-data', component: DeleteDataPage,
    meta: meta('en', 'Delete account and data — Palabravo', 'Request deletion of your Palabravo account and associated data.', '/eliminar-datos', '/en/delete-data'),
  },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]
