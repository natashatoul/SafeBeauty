import { describe, expect, it } from 'vitest'
import { getPlainLanguageFunctionDescription } from './cosmeticFunctionDescriptions.js'

describe('getPlainLanguageFunctionDescription', () => {
  it('explains anti-seborrheic care in plain language', () => {
    expect(getPlainLanguageFunctionDescription('ANTI-SEBORRHEIC'))
      .toBe('Helps care for oily, flaky skin or scalp, including dandruff-prone areas.')
  })

  it('matches function names without case sensitivity', () => {
    expect(getPlainLanguageFunctionDescription('solvent'))
      .toContain('dissolve other ingredients')
  })

  it('explains miscellaneous skin conditioning without repeating the technical term', () => {
    const description = getPlainLanguageFunctionDescription('SKIN CONDITIONING - MISCELLANEOUS')

    expect(description).toContain('softness and smoothness')
    expect(description.toLowerCase()).not.toContain('skin conditioning')
    expect(description).not.toContain('CosIng')
  })

  it('returns an empty description for an unknown function', () => {
    expect(getPlainLanguageFunctionDescription('UNLISTED FUNCTION')).toBe('')
  })
})
