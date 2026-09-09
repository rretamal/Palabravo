<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const code = computed(() => /^puzzle-(0[1-9]|[12][0-9]|30)$/.test(String(route.params.id)) ? String(route.params.id).slice(-2) : null)
</script>

<template>
  <main class="section">
    <h1>{{ code ? `Te retaron a resolver el reto ${code}` : 'Reto no encontrado' }}</h1>
    <template v-if="code">
      <p>Encuentra cuatro conexiones entre dieciséis palabras. Sin revelar las respuestas de tu amigo.</p>
      <p>Si tienes Palabravo, abre este enlace desde tu mensaje. Después de instalar, elige «Tengo un código de reto» e ingresa <strong>{{ code }}</strong>.</p>
      <a href="https://play.google.com/store/apps/details?id=com.palabravo.app">Instalar en Google Play</a>
      <p>La versión para iPhone se anunciará cuando esté disponible en App Store.</p>
    </template>
    <RouterLink to="/">Conocer Palabravo</RouterLink>
  </main>
</template>
