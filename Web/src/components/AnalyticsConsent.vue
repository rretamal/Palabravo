<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { readConsent, setConsent } from '../analytics'
const visible = ref(false)
const route = useRoute()
const en = computed(() => route.meta.locale === 'en')
function choose(value: 'granted' | 'denied') { setConsent(value); visible.value = false }
onMounted(() => {
  visible.value = readConsent() === null
  window.addEventListener('palabravo-analytics-settings', () => { visible.value = true })
})
</script>
<template>
  <aside v-if="visible" class="analytics-consent" :aria-label="en ? 'Website analytics preferences' : 'Preferencias de analítica web'">
    <p>{{ en ? 'May we use Google Analytics to understand visits and clicks to Google Play? It is optional. You can change your choice in the footer.' : '¿Nos permites usar Google Analytics para entender las visitas y los clics hacia Google Play? Es opcional. Puedes cambiar tu elección en el pie de página.' }}</p>
    <div>
      <button class="button button-small" @click="choose('denied')">{{ en ? 'Reject' : 'Rechazar' }}</button>
      <button class="button button-small" @click="choose('granted')">{{ en ? 'Accept analytics' : 'Aceptar analítica' }}</button>
      <RouterLink :to="en ? '/en/privacy' : '/privacidad'">{{ en ? 'Privacy' : 'Privacidad' }}</RouterLink>
    </div>
  </aside>
</template>
<style scoped>
.analytics-consent { position: fixed; bottom: 16px; left: 16px; right: 16px; max-width: 760px; margin: auto; z-index: 100; padding: 20px; background: #fbf7f0; color: #292b33; border: 2px solid #292b33; border-radius: 16px; box-shadow: 0 4px 24px #0002; }
.analytics-consent p { margin: 0 0 14px; }
.analytics-consent div { display: flex; gap: 12px; flex-wrap: wrap; align-items: center; }
</style>
