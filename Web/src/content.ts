export type Locale = 'es' | 'en'

export const playStoreUrl = 'https://play.google.com/store/apps/details?id=com.palabravo.app'
export const contactEmail = 'hello@palabravo.app'

export function localeFromPath(path: string): Locale {
  return path === '/en' || path.startsWith('/en/') ? 'en' : 'es'
}

export const copy = {
  es: {
    nav: { home: 'Inicio', daily: 'Reto diario', how: 'Cómo jugar', path: 'Tu camino', download: 'Descargar' },
    hero: {
      eyebrow: 'Juego de palabras en español',
      titleA: 'Descubre conexiones.',
      titleB: 'Domina las palabras.',
      lead: '16 palabras. 4 grupos. Retos diarios, medallas y un camino de maestría que siempre tiene algo nuevo.',
      download: 'Descargar en Google Play',
      imageAlt: 'Reto de Palabravo con una cuadrícula de palabras',
      mascotAlt: 'Mascota de Palabravo',
      note: 'Pequeños desafíos.\nGrandes mentes.',
      stats: [['30', 'retos para pensar'], ['6', 'rangos por conquistar'], ['1', 'reto nuevo cada día']],
    },
    daily: { eyebrow: 'Reto diario', title: 'Una conexión nueva cada día.', text: 'La fecha elige el mismo reto para todos. Juega, compara tu resultado y vuelve mañana.', cta: 'Jugar el reto de hoy' },
    how: {
      eyebrow: 'Cómo funciona', title: 'Simple, pero adictivo.', text: 'Observa las palabras, detecta patrones y forma cuatro grupos relacionados.',
      steps: [
        ['01', 'Observa', 'Lee las 16 palabras y busca ideas que aparezcan más de una vez.'],
        ['02', 'Conecta', 'Selecciona cuatro palabras que compartan una relación precisa.'],
        ['03', 'Avanza', 'Completa el reto, gana una medalla y desbloquea el siguiente.'],
      ],
    },
    path: {
      eyebrow: 'Tu progreso', title: 'Un camino hasta Gran Maestro.', text: 'Completa 30 retos, sube de rango y construye tu propia racha.',
      ranks: [['Novato', 'Comienza'], ['Ágil', '5 retos'], ['Ingenioso', '10 retos'], ['Experto', '15 retos'], ['Maestro', '20 retos'], ['Gran Maestro', '25 retos']],
    },
    benefits: {
      eyebrow: 'Entrena jugando', title: 'Cada partida deja algo.',
      cards: [
        ['Vocabulario en contexto', 'Aprende relaciones entre palabras, significados y usos sin memorizar listas.'],
        ['Partidas breves', 'Un reto cabe en unos minutos y funciona bien como pausa mental diaria.'],
        ['Progreso visible', 'Medallas, rachas y rangos hacen claro cuánto has avanzado.'],
      ],
    },
    closing: { eyebrow: '¿Listo para el reto?', title: 'Descarga Palabravo', text: 'Gratis en Google Play.', cta: 'Descargar en Google Play' },
    footer: { privacy: 'Privacidad', deletion: 'Eliminar mis datos', terms: 'Términos de uso', contact: 'Contacto', rights: 'Todos los derechos reservados.' },
  },
  en: {
    nav: { home: 'Home', daily: 'Daily challenge', how: 'How to play', path: 'Your path', download: 'Download' },
    hero: {
      eyebrow: 'A Spanish word game',
      titleA: 'Spot the connections.',
      titleB: 'Build your Spanish.',
      lead: '16 Spanish words. 4 groups. Daily challenges, medals, and a mastery path that turns vocabulary practice into play.',
      download: 'Get it on Google Play',
      imageAlt: 'A Palabravo challenge showing a grid of Spanish words',
      mascotAlt: 'Palabravo mascot',
      note: 'Small challenges.\nBigger vocabulary.',
      stats: [['30', 'challenges to solve'], ['6', 'ranks to master'], ['1', 'new daily challenge']],
    },
    daily: { eyebrow: 'Daily challenge', title: 'A fresh connection every day.', text: 'The date picks the same puzzle for everyone. Play, compare your result, and come back tomorrow.', cta: 'Play today’s challenge' },
    how: {
      eyebrow: 'How it works', title: 'Simple, then surprisingly deep.', text: 'Read the Spanish words, notice patterns, and build four related groups.',
      steps: [
        ['01', 'Observe', 'Scan all 16 Spanish words and look for ideas that overlap.'],
        ['02', 'Connect', 'Choose four words that share one exact relationship.'],
        ['03', 'Advance', 'Finish the puzzle, earn a medal, and unlock the next challenge.'],
      ],
    },
    path: {
      eyebrow: 'Your progress', title: 'A path to Gran Maestro.', text: 'Complete 30 challenges, move through six ranks, and build a lasting Spanish practice habit.',
      ranks: [['Novato', 'Beginner'], ['Ágil', 'Quick'], ['Ingenioso', 'Resourceful'], ['Experto', 'Expert'], ['Maestro', 'Master'], ['Gran Maestro', 'Grandmaster']],
    },
    benefits: {
      eyebrow: 'Practice through play', title: 'Every round teaches you something.',
      cards: [
        ['Vocabulary in context', 'Learn how Spanish words relate by meaning and use, without memorizing lists.'],
        ['Short sessions', 'A puzzle fits into a few minutes and makes an easy daily language break.'],
        ['Visible progress', 'Medals, streaks, and ranks show how far your Spanish practice has come.'],
      ],
    },
    closing: { eyebrow: 'Ready for the challenge?', title: 'Download Palabravo', text: 'Free on Google Play.', cta: 'Get it on Google Play' },
    footer: { privacy: 'Privacy', deletion: 'Delete my data', terms: 'Terms of use', contact: 'Contact', rights: 'All rights reserved.' },
  },
} as const

export function localizedPaths(locale: Locale) {
  return locale === 'es'
    ? { home: '/', privacy: '/privacidad', deletion: '/eliminar-datos', terms: '/terminos', alternate: '/en' }
    : { home: '/en', privacy: '/en/privacy', deletion: '/en/delete-data', terms: '/en/terms', alternate: '/' }
}
