<script setup lang="ts">
import { onMounted, ref } from 'vue'

type Weekly = { title: string; subtitle: string; flag: string; startsAt: string; endsAt: string; badge: { name: string; imageUrl: string } }
const weekly = ref<Weekly | null>(null)
const unavailable = ref(false)
const playUrl = ref(`intent://palabravo.app/semanal#Intent;scheme=https;package=com.palabravo.app;S.browser_fallback_url=${encodeURIComponent('https://play.google.com/store/apps/details?id=com.palabravo.app')};end`)
onMounted(async () => {
  if (/iPhone|iPad|iPod/i.test(navigator.userAgent)) playUrl.value = 'palabravo://semanal'
  try {
    const response = await fetch('/content/weekly.json')
    if (!response.ok) throw new Error()
    weekly.value = await response.json() as Weekly
  } catch { unavailable.value = true }
})
</script>

<template>
  <main class="weekly-page page-width">
    <section v-if="weekly" class="weekly-hero">
      <span class="weekly-flag">{{ weekly.flag }}</span>
      <p class="eyebrow">RETO DE LA SEMANA</p>
      <h1>{{ weekly.title }}</h1>
      <p class="weekly-lead">{{ weekly.subtitle }}</p>
      <div class="weekly-facts"><span>16 palabras</span><span>4 conexiones</span><span>Badge especial</span></div>
      <img :src="weekly.badge.imageUrl" :alt="`Badge ${weekly.badge.name}`" class="weekly-badge">
      <p>Abre Palabravo para jugar el mismo reto que toda la comunidad.</p>
      <a class="button" :href="playUrl">Jugar en Palabravo</a>
      <a class="weekly-store" href="https://play.google.com/store/apps/details?id=com.palabravo.app">Disponible en Google Play</a>
    </section>
    <section v-else class="weekly-hero"><h1>{{ unavailable ? 'Reto no disponible' : 'Preparando el reto…' }}</h1></section>
  </main>
</template>
