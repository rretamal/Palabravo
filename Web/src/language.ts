import type { Locale } from './content'

export const languagePreferenceKey = 'palabravo-language'

export function preferredLanguage(saved: string | null, languages: readonly string[]): Locale {
  if (saved === 'es' || saved === 'en') return saved
  for (const language of languages) {
    const base = language.toLowerCase().split('-')[0]
    if (base === 'es' || base === 'en') return base
  }
  return 'es'
}

export function saveLanguage(locale: Locale) {
  try { localStorage.setItem(languagePreferenceKey, locale) } catch { /* Storage may be unavailable. */ }
}
