import { describe, expect, it } from 'vitest'
import { preferredLanguage } from './language'

describe('language selection', () => {
  it('honors manual choices before browser preferences', () => {
    expect(preferredLanguage('es', ['en-US'])).toBe('es')
    expect(preferredLanguage('en', ['es-CL'])).toBe('en')
  })
  it('uses the first supported browser language including regional variants', () => {
    expect(preferredLanguage(null, ['en-GB', 'es'])).toBe('en')
    expect(preferredLanguage(null, ['es-CL', 'en'])).toBe('es')
    expect(preferredLanguage(null, ['fr', 'en-US'])).toBe('en')
  })
  it('defaults to Spanish when no supported preference exists', () => {
    expect(preferredLanguage(null, [])).toBe('es')
    expect(preferredLanguage('invalid', ['fr'])).toBe('es')
  })
})
