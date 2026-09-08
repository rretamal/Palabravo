<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { copy, localizedPaths, playStoreUrl, type Locale } from '../content'

const props = defineProps<{ locale: Locale; compact?: boolean }>()
const text = copy[props.locale]
const paths = localizedPaths(props.locale)
const route = useRoute()
const alternatePath = computed(() => props.locale === 'es'
  ? String(route.meta.enPath ?? '/en')
  : String(route.meta.esPath ?? '/'))
</script>

<template>
  <header class="site-header page-width">
    <RouterLink class="brand" :to="paths.home" :aria-label="`Palabravo, ${text.nav.home}`">
      <span class="brand-mark" aria-hidden="true">P</span>
      <span>Palabravo</span>
    </RouterLink>
    <nav v-if="!compact" :aria-label="props.locale === 'es' ? 'Navegación principal' : 'Main navigation'">
      <RouterLink :to="`${paths.home}#inicio`">{{ text.nav.home }}</RouterLink>
      <RouterLink :to="`${paths.home}#diario`">{{ text.nav.daily }}</RouterLink>
      <RouterLink :to="`${paths.home}#como-jugar`">{{ text.nav.how }}</RouterLink>
      <RouterLink :to="`${paths.home}#progreso`">{{ text.nav.path }}</RouterLink>
    </nav>
    <div class="header-actions">
      <RouterLink class="language-link" :to="alternatePath" :aria-label="props.locale === 'es' ? 'View in English' : 'Ver en español'">
        {{ props.locale === 'es' ? 'EN' : 'ES' }}
      </RouterLink>
      <a v-if="!compact" class="button button-small" :href="playStoreUrl">{{ text.nav.download }}</a>
    </div>
  </header>
</template>
