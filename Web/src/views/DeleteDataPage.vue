<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import SiteFooter from '../components/SiteFooter.vue'
import SiteHeader from '../components/SiteHeader.vue'
import { contactEmail, localeFromPath, type Locale } from '../content'

const route = useRoute()
const locale = computed(() => localeFromPath(route.path))
const startedAt = ref(0)
const status = ref<'idle' | 'sending' | 'success' | 'error'>('idle')
const reference = ref('')
const form = reactive({ email: '', identifierType: 'alias', identifier: '', details: '', website: '', consent: false })

onMounted(() => { startedAt.value = Date.now() })

const text: Record<Locale, Record<string, string>> = {
  es: {
    eyebrow: 'Tu cuenta', title: 'Eliminar cuenta y datos', intro: 'Puedes borrar tu cuenta de Palabravo y la información asociada. La forma más rápida y segura es hacerlo desde la app.',
    preferred: 'Opción recomendada', appTitle: 'Desde la app', appText: 'Abre Perfil → Eliminar cuenta y datos. Confirma dos veces y Palabravo enviará una solicitud autenticada directamente a PlayFab.',
    webTitle: '¿Ya no tienes acceso a la app?', webText: 'Envía esta solicitud manual. Te contactaremos para verificar que podamos identificar la cuenta correcta.',
    email: 'Correo de contacto', type: '¿Cómo te identificamos?', alias: 'Alias de Palabravo', google: 'Perfil de Google Play Games', identifier: 'Alias o nombre de jugador', details: 'Detalles adicionales (opcional)', detailsHelp: 'Por ejemplo, cuándo jugaste por última vez. No incluyas contraseñas ni códigos.',
    consent: 'Confirmo que solicito eliminar mi cuenta de Palabravo y los datos asociados.', submit: 'Enviar solicitud de eliminación', sending: 'Enviando…',
    success: 'Recibimos tu solicitud', successText: 'Te enviamos una confirmación por correo. Conserva este número de referencia:', error: 'No pudimos enviar la solicitud. Revisa los datos e inténtalo nuevamente.',
    timeline: 'Atenderemos la solicitud en un máximo de 30 días. Puede que necesitemos información adicional para verificar la cuenta. La eliminación no afecta tu cuenta general de Google Play Games.',
    contact: 'También puedes escribir directamente a', required: 'Todos los campos marcados son obligatorios.',
  },
  en: {
    eyebrow: 'Your account', title: 'Delete account and data', intro: 'You can delete your Palabravo account and associated information. The fastest and safest method is inside the app.',
    preferred: 'Recommended option', appTitle: 'Inside the app', appText: 'Open Profile → Delete account and data. Confirm twice and Palabravo will send an authenticated request directly to PlayFab.',
    webTitle: 'No longer have access to the app?', webText: 'Send this manual request. We will contact you to verify that we can identify the correct account.',
    email: 'Contact email', type: 'How can we identify you?', alias: 'Palabravo alias', google: 'Google Play Games profile', identifier: 'Alias or player name', details: 'Additional details (optional)', detailsHelp: 'For example, when you last played. Do not include passwords or codes.',
    consent: 'I confirm that I am requesting deletion of my Palabravo account and associated data.', submit: 'Send deletion request', sending: 'Sending…',
    success: 'We received your request', successText: 'We sent a confirmation by email. Keep this reference number:', error: 'We could not send the request. Check your information and try again.',
    timeline: 'We will handle the request within 30 days. We may need more information to verify the account. Deletion does not affect your general Google Play Games account.',
    contact: 'You may also email us directly at', required: 'All marked fields are required.',
  },
}

const t = computed(() => text[locale.value])

async function submit() {
  status.value = 'sending'
  reference.value = ''
  try {
    const response = await fetch('/api/deletion-requests', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...form, locale: locale.value, startedAt: startedAt.value }),
    })
    if (!response.ok) throw new Error('request failed')
    const result = await response.json() as { requestId: string }
    reference.value = result.requestId
    status.value = 'success'
  } catch {
    status.value = 'error'
  }
}
</script>

<template>
  <div class="site-shell">
    <SiteHeader :locale="locale" compact />
    <main class="delete-main page-width">
      <header class="delete-heading"><p class="eyebrow coral-text">{{ t.eyebrow }}</p><h1>{{ t.title }}</h1><p>{{ t.intro }}</p></header>
      <section class="app-delete-card"><span>{{ t.preferred }}</span><div class="app-delete-icon" aria-hidden="true">P</div><div><h2>{{ t.appTitle }}</h2><p>{{ t.appText }}</p></div></section>
      <section class="request-panel">
        <div class="request-copy"><h2>{{ t.webTitle }}</h2><p>{{ t.webText }}</p><p>{{ t.timeline }}</p><p>{{ t.contact }} <a :href="`mailto:${contactEmail}`">{{ contactEmail }}</a>.</p></div>
        <form v-if="status !== 'success'" class="deletion-form" @submit.prevent="submit">
          <p class="form-note">{{ t.required }}</p>
          <label>{{ t.email }} *<input v-model.trim="form.email" required type="email" autocomplete="email" maxlength="160" /></label>
          <label>{{ t.type }} *<select v-model="form.identifierType" required><option value="alias">{{ t.alias }}</option><option value="google">{{ t.google }}</option></select></label>
          <label>{{ t.identifier }} *<input v-model.trim="form.identifier" required minlength="3" maxlength="120" autocomplete="off" /></label>
          <label>{{ t.details }}<textarea v-model.trim="form.details" maxlength="500" rows="4"></textarea><small>{{ t.detailsHelp }}</small></label>
          <label class="honeypot" aria-hidden="true">Website<input v-model="form.website" tabindex="-1" autocomplete="off" /></label>
          <label class="check-label"><input v-model="form.consent" required type="checkbox" /><span>{{ t.consent }}</span></label>
          <button class="button" type="submit" :disabled="status === 'sending'">{{ status === 'sending' ? t.sending : t.submit }}</button>
          <p v-if="status === 'error'" class="form-error" role="alert">{{ t.error }}</p>
        </form>
        <div v-else class="success-card" role="status"><span aria-hidden="true">✓</span><h2>{{ t.success }}</h2><p>{{ t.successText }}</p><strong>{{ reference }}</strong></div>
      </section>
    </main>
    <SiteFooter :locale="locale" />
  </div>
</template>
