<script setup lang="ts">
import { computed } from 'vue'
import { contactEmail, copy, localizedPaths, type Locale } from '../content'

const props = defineProps<{ locale: Locale }>()
const text = computed(() => copy[props.locale])
const paths = computed(() => localizedPaths(props.locale))
function analyticsSettings() { window.dispatchEvent(new Event('palabravo-analytics-settings')) }
</script>

<template>
  <footer class="site-footer">
    <div class="footer-inner page-width">
      <RouterLink class="brand" :to="paths.home"><span class="brand-mark" aria-hidden="true">P</span><span>Palabravo</span></RouterLink>
      <nav :aria-label="props.locale === 'es' ? 'Información legal' : 'Legal information'">
        <RouterLink :to="paths.privacy">{{ text.footer.privacy }}</RouterLink>
        <RouterLink :to="paths.deletion">{{ text.footer.deletion }}</RouterLink>
        <RouterLink :to="paths.terms">{{ text.footer.terms }}</RouterLink>
        <button class="language-link" @click="analyticsSettings">{{ props.locale === 'es' ? 'Preferencias de analítica' : 'Analytics preferences' }}</button>
        <a :href="`mailto:${contactEmail}`">{{ text.footer.contact }}</a>
      </nav>
      <p>© {{ new Date().getFullYear() }} Palabravo. {{ text.footer.rights }}</p>
    </div>
  </footer>
</template>
