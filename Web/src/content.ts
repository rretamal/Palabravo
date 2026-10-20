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
      titleA: 'Conecta palabras.',
      titleB: 'Descubre más del español.',
      lead: 'Pon a prueba tu vocabulario, descubre expresiones y responde preguntas con retos de palabras y cultura. Para hispanohablantes y para quienes quieren practicar español jugando.',
      download: 'Descargar en Google Play',
      imageAlt: 'Reto de Palabravo con una cuadrícula de palabras',
      mascotAlt: 'Mascota de Palabravo',
      note: 'Pequeños desafíos.\nGrandes mentes.',
      stats: [['30', 'retos para pensar'], ['6', 'rangos por conquistar'], ['1', 'reto nuevo cada día']],
    },
    daily: { eyebrow: 'Reto diario', title: 'Una nueva forma de jugar con el español.', text: 'Practica palabras, expresiones y comprensión con el reto de cada fecha. Juega a tu ritmo y vuelve mañana.', cta: 'Jugar el reto de hoy' },
    how: {
      eyebrow: 'Cómo funciona', title: 'Así se juegan las conexiones.', text: 'Observa las palabras, detecta patrones y forma cuatro grupos relacionados.',
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
    formats: {
      eyebrow: 'Palabras, preguntas y cultura', title: '¿Qué vas a encontrar?',
      intro: 'Si hablas español, desafía lo que sabes. Si ya lo entiendes y quieres practicarlo, ejercita la lectura y el vocabulario a tu ritmo.',
      cards: [
        ['Conexiones entre palabras', '¿Qué une a guitarra, violín, arpa y violonchelo? Encuentra relaciones entre significados y usos.'],
        ['Preguntas en español', '¿Qué proceso convierte agua líquida en vapor? Lee, piensa y elige una respuesta en los retos de trivia.'],
        ['Expresiones y cultura', 'Descubre dobles sentidos y referencias locales en especiales como Chile entre líneas. El contenido varía según el reto.'],
      ],
    },
    benefits: {
      eyebrow: 'Entrena jugando', title: 'Cada partida deja algo.',
      cards: [
        ['Vocabulario en contexto', 'Aprende relaciones entre palabras, significados y usos sin memorizar listas.'],
        ['Partidas breves', 'Un reto cabe en unos minutos y funciona bien como pausa mental diaria.'],
        ['Progreso visible', 'Medallas, rachas y rangos hacen claro cuánto has avanzado.'],
      ],
    },
    closing: { eyebrow: '¿Listo para el reto?', title: 'Descarga Palabravo', text: 'Gratis en Android, con anuncios.', cta: 'Descargar en Google Play' },
    footer: { privacy: 'Privacidad', deletion: 'Eliminar mis datos', terms: 'Términos de uso', contact: 'Contacto', rights: 'Todos los derechos reservados.' },
  },
  en: {
    nav: { home: 'Home', daily: 'Daily challenge', how: 'How to play', path: 'Your path', download: 'Download' },
    hero: {
      eyebrow: 'A Spanish word game',
      titleA: 'Spot the connections.',
      titleB: 'Build your Spanish.',
      lead: 'Explore vocabulary, expressions, and trivia through word and culture challenges. For Spanish speakers and anyone who wants to practice Spanish through play.',
      download: 'Get it on Google Play',
      imageAlt: 'A Palabravo challenge showing a grid of Spanish words',
      mascotAlt: 'Palabravo mascot',
      note: 'Small challenges.\nBigger vocabulary.',
      stats: [['30', 'challenges to solve'], ['6', 'ranks to master'], ['1', 'new daily challenge']],
    },
    daily: { eyebrow: 'Daily challenge', title: 'A fresh way to play with Spanish.', text: 'Practice vocabulary, expressions, and comprehension with a dated challenge. Play at your own pace and come back tomorrow.', cta: 'Play today’s challenge' },
    how: {
      eyebrow: 'How it works', title: 'How word connections work.', text: 'Read the Spanish words, notice patterns, and build four related groups.',
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
    formats: {
      eyebrow: 'Words, questions, and culture', title: 'What will you discover?',
      intro: 'Already speak Spanish? Put your knowledge to the test. If you understand Spanish and want more practice, explore reading and vocabulary at your own pace. Challenges are in Spanish.',
      cards: [
        ['Word connections', 'What connects guitarra, violín, arpa, and violonchelo? Discover relationships between Spanish words and their meanings.'],
        ['Trivia in Spanish', '¿Qué proceso convierte agua líquida en vapor? Read questions in Spanish, think, and choose your answer.'],
        ['Expressions and culture', 'Explore double meanings and local references in specials such as Chile entre líneas. Content varies by challenge.'],
      ],
    },
    benefits: {
      eyebrow: 'Practice through play', title: 'Every round teaches you something.',
      cards: [
        ['Vocabulary in context', 'Learn how Spanish words relate by meaning and use, without memorizing lists.'],
        ['Short sessions', 'A puzzle fits into a few minutes and makes an easy daily language break.'],
        ['Visible progress', 'Medals, streaks, and ranks show how far your Spanish practice has come.'],
      ],
    },
    closing: { eyebrow: 'Ready for the challenge?', title: 'Download Palabravo', text: 'Free on Android, with ads. Challenges are in Spanish.', cta: 'Get it on Google Play' },
    footer: { privacy: 'Privacy', deletion: 'Delete my data', terms: 'Terms of use', contact: 'Contact', rights: 'All rights reserved.' },
  },
} as const

export function localizedPaths(locale: Locale) {
  return locale === 'es'
    ? { home: '/', privacy: '/privacidad', deletion: '/eliminar-datos', terms: '/terminos', alternate: '/en' }
    : { home: '/en', privacy: '/en/privacy', deletion: '/en/delete-data', terms: '/en/terms', alternate: '/' }
}
