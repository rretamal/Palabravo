<script setup lang="ts">
import { contactEmail, copy, localizedPaths, type Locale } from '../content'

const props = defineProps<{ locale: Locale }>()
const text = copy[props.locale]
const paths = localizedPaths(props.locale)
</script>

<template>
  <footer class="site-footer">
    <div class="footer-inner page-width">
      <RouterLink class="brand" :to="paths.home"><span class="brand-mark" aria-hidden="true">P</span><span>Palabravo</span></RouterLink>
      <nav :aria-label="props.locale === 'es' ? 'Información legal' : 'Legal information'">
        <RouterLink :to="paths.privacy">{{ text.footer.privacy }}</RouterLink>
        <RouterLink :to="paths.deletion">{{ text.footer.deletion }}</RouterLink>
        <RouterLink :to="paths.terms">{{ text.footer.terms }}</RouterLink>
        <a :href="`mailto:${contactEmail}`">{{ text.footer.contact }}</a>
      </nav>
      <p>© {{ new Date().getFullYear() }} Palabravo. {{ text.footer.rights }}</p>
    </div>
  </footer>
</template>
