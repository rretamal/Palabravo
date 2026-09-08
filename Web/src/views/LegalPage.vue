<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import SiteFooter from '../components/SiteFooter.vue'
import SiteHeader from '../components/SiteHeader.vue'
import { localeFromPath } from '../content'
import { legalDocuments, type LegalKind } from '../legalContent'

const props = defineProps<{ kind: LegalKind }>()
const route = useRoute()
const locale = computed(() => localeFromPath(route.path))
const document = computed(() => legalDocuments[locale.value][props.kind])
</script>

<template>
  <div class="site-shell">
    <SiteHeader :locale="locale" compact />
    <main class="legal-main page-width">
      <div class="legal-heading"><p class="eyebrow coral-text">{{ document.eyebrow }}</p><h1>{{ document.title }}</h1><p class="legal-intro">{{ document.intro }}</p><small>{{ document.updated }}</small></div>
      <article class="legal-card">
        <section v-for="section in document.sections" :key="section.title">
          <h2>{{ section.title }}</h2>
          <p v-for="paragraph in section.paragraphs" :key="paragraph">{{ paragraph }}</p>
          <ul v-if="section.items"><li v-for="item in section.items" :key="item">{{ item }}</li></ul>
        </section>
      </article>
    </main>
    <SiteFooter :locale="locale" />
  </div>
</template>
