<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useHead } from '@unhead/vue'
import { useRoute, useRouter } from 'vue-router'
import { languagePreferenceKey, preferredLanguage } from './language'

const route = useRoute()
const router = useRouter()
onMounted(() => {
  // Only the default landing negotiates language; explicit and shared URLs stay intact.
  if (route.path !== '/') return
  let saved: string | null = null
  try { saved = localStorage.getItem(languagePreferenceKey) } catch { /* Use browser preferences. */ }
  if (preferredLanguage(saved, navigator.languages?.length ? navigator.languages : [navigator.language]) === 'en') {
    void router.replace({ path: '/en', query: route.query, hash: route.hash })
  }
})
const siteOrigin = 'https://palabravo.app'
useHead(computed(() => {
  const locale = route.meta.locale === 'en' ? 'en' : 'es'
  const title = String(route.meta.title ?? 'Palabravo')
  const description = String(route.meta.description ?? '')
  const currentPath = route.path === '/' ? '/' : route.path
  return {
    htmlAttrs: { lang: locale },
    title,
    meta: [
      { name: 'description', content: description },
      { property: 'og:title', content: title },
      { property: 'og:description', content: description },
      { property: 'og:type', content: 'website' },
      { property: 'og:url', content: `${siteOrigin}${currentPath}` },
      { name: 'twitter:card', content: 'summary' },
    ],
    link: [
      { rel: 'canonical', href: `${siteOrigin}${currentPath}` },
      { rel: 'alternate', hreflang: 'es', href: `${siteOrigin}${String(route.meta.esPath ?? '/')}` },
      { rel: 'alternate', hreflang: 'en', href: `${siteOrigin}${String(route.meta.enPath ?? '/en')}` },
      { rel: 'alternate', hreflang: 'x-default', href: `${siteOrigin}${String(route.meta.esPath ?? '/')}` },
    ],
  }
}))
</script>

<template>
  <RouterView />
</template>
