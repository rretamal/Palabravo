<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import SiteFooter from '../components/SiteFooter.vue'
import SiteHeader from '../components/SiteHeader.vue'
import { copy, localeFromPath, playStoreUrl } from '../content'

const route = useRoute()
const locale = computed(() => localeFromPath(route.path))
const text = computed(() => copy[locale.value])
const rankColors = ['coral', 'silver', 'mustard', 'sage', 'blue', 'purple']
</script>

<template>
  <div class="site-shell">
    <SiteHeader :locale="locale" />
    <main id="inicio">
      <section class="hero page-width">
        <div class="hero-copy reveal">
          <p class="eyebrow">{{ text.hero.eyebrow }}</p>
          <h1>{{ text.hero.titleA }}<br />{{ text.hero.titleB }}</h1>
          <p class="hero-lead">{{ text.hero.lead }}</p>
          <a class="button" :href="playStoreUrl">{{ text.hero.download }} <span aria-hidden="true">→</span></a>
          <ul class="quick-benefits" :aria-label="locale === 'es' ? 'Palabravo en cifras' : 'Palabravo at a glance'">
            <li v-for="stat in text.hero.stats" :key="stat[1]"><strong>{{ stat[0] }}</strong><span>{{ stat[1] }}</span></li>
          </ul>
        </div>
        <div class="hero-art" :aria-label="locale === 'es' ? 'Vista de Palabravo en Android' : 'Palabravo on Android'">
          <div class="hero-blob hero-blob-one"></div><div class="hero-blob hero-blob-two"></div>
          <img class="phone-shot" :src="locale === 'es' ? '/assets/phone-es.webp' : '/assets/phone-en.webp'" :alt="text.hero.imageAlt" width="1080" height="1920" />
          <img class="mascot" src="/assets/mascot.webp" :alt="text.hero.mascotAlt" width="771" height="900" />
          <p class="hand-note">{{ text.hero.note }}</p>
        </div>
      </section>

      <section id="diario" class="daily-card page-width reveal">
        <div class="daily-icon" aria-hidden="true">✦</div>
        <div><p class="eyebrow">{{ text.daily.eyebrow }}</p><h2>{{ text.daily.title }}</h2><p>{{ text.daily.text }}</p></div>
        <a class="button" :href="playStoreUrl">{{ text.daily.cta }} <span aria-hidden="true">→</span></a>
      </section>

      <section id="como-jugar" class="section page-width how-section">
        <div class="section-intro"><p class="eyebrow coral-text">{{ text.how.eyebrow }}</p><h2>{{ text.how.title }}</h2><p>{{ text.how.text }}</p></div>
        <ol class="steps">
          <li v-for="step in text.how.steps" :key="step[0]" class="reveal"><span class="step-number">{{ step[0] }}</span><div class="tile-demo" aria-hidden="true"><i v-for="n in 16" :key="n"></i></div><h3>{{ step[1] }}</h3><p>{{ step[2] }}</p></li>
        </ol>
      </section>

      <section id="progreso" class="path-section">
        <div class="page-width path-layout">
          <div class="path-phone" aria-hidden="true"><img :src="locale === 'es' ? '/assets/phone-es.webp' : '/assets/phone-en.webp'" alt="" width="1080" height="1920" loading="lazy" /></div>
          <div class="path-copy"><p class="eyebrow coral-text">{{ text.path.eyebrow }}</p><h2>{{ text.path.title }}</h2><p>{{ text.path.text }}</p>
            <ol class="ranks" :aria-label="locale === 'es' ? 'Seis rangos de Palabravo' : 'Six Palabravo ranks'">
              <li v-for="(rank, index) in text.path.ranks" :key="rank[0]"><span class="rank-medal" :class="rankColors[index]">✦</span><strong>{{ rank[0] }}</strong><small>{{ rank[1] }}</small></li>
            </ol>
          </div>
        </div>
      </section>

      <section class="section benefits-section page-width">
        <div class="section-intro centered"><p class="eyebrow coral-text">{{ text.benefits.eyebrow }}</p><h2>{{ text.benefits.title }}</h2></div>
        <div class="benefit-grid">
          <article v-for="(card, index) in text.benefits.cards" :key="card[0]" class="benefit-card reveal"><span aria-hidden="true">0{{ index + 1 }}</span><h3>{{ card[0] }}</h3><p>{{ card[1] }}</p></article>
        </div>
      </section>

      <section class="closing-section">
        <div class="closing-inner page-width"><img src="/assets/mascot.webp" alt="" width="771" height="900" loading="lazy" /><div><p class="eyebrow coral-text">{{ text.closing.eyebrow }}</p><h2>{{ text.closing.title }}</h2><p>{{ text.closing.text }}</p></div><a class="button" :href="playStoreUrl">{{ text.closing.cta }}</a></div>
      </section>
    </main>
    <SiteFooter :locale="locale" />
  </div>
</template>
