<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

type Invite = { token: string; weeklyId: string; challenger: string; score: number; timeSeconds: number; mistakes: number; medal: string }
type Weekly = { id: string; title: string; subtitle: string; flag: string }
const route = useRoute()
const invite = ref<Invite | null>(null)
const weekly = ref<Weekly | null>(null)
const failed = ref(false)
const token = computed(() => String(route.params.token).toUpperCase())
const time = computed(() => invite.value ? `${Math.floor(invite.value.timeSeconds / 60)}:${String(invite.value.timeSeconds % 60).padStart(2, '0')}` : '')
const medal = computed(() => ({ gold: '🥇 Oro', silver: '🥈 Plata', bronze: '🥉 Bronce' })[invite.value?.medal ?? ''] ?? '')
const isAppleMobile = ref(false)
const acceptUrl = computed(() => isAppleMobile.value ? `palabravo://challenge/${token.value}` : `intent://palabravo.app/challenge/${token.value}#Intent;scheme=https;package=com.palabravo.app;S.browser_fallback_url=${encodeURIComponent('https://play.google.com/store/apps/details?id=com.palabravo.app')};end`)
onMounted(async () => {
  isAppleMobile.value = /iPhone|iPad|iPod/i.test(navigator.userAgent)
  if (!/^[A-Z0-9]{6}$/.test(token.value)) { failed.value = true; return }
  try {
    const [inviteResponse, weeklyResponse] = await Promise.all([
      fetch(`/api/challenges/${token.value}`), fetch('/content/weekly.json'),
    ])
    if (!inviteResponse.ok || !weeklyResponse.ok) throw new Error()
    invite.value = await inviteResponse.json() as Invite
    weekly.value = await weeklyResponse.json() as Weekly
    if (invite.value.weeklyId !== weekly.value.id) throw new Error()
  } catch { invite.value = null; weekly.value = null; failed.value = true }
})
</script>

<template>
  <main class="weekly-page page-width">
    <section v-if="invite && weekly" class="weekly-hero challenge-card">
      <span class="weekly-flag">{{ weekly.flag }}</span>
      <p class="eyebrow">{{ weekly.title }}</p>
      <h1>{{ invite.challenger }} te desafió</h1>
      <p class="weekly-lead">¿Puedes superar su resultado?</p>
      <div class="challenge-result"><strong>{{ medal }}</strong><span>{{ time }} · {{ invite.mistakes }} errores</span></div>
      <a class="button" :href="acceptUrl">Aceptar desafío</a>
      <a class="weekly-store" href="https://play.google.com/store/apps/details?id=com.palabravo.app">Instalar desde Google Play</a>
    </section>
    <section v-else class="weekly-hero"><h1>{{ failed ? 'Desafío no disponible' : 'Buscando desafío…' }}</h1><RouterLink to="/semanal">Ver el reto semanal</RouterLink></section>
  </main>
</template>
