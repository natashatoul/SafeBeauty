import { describe, expect, it } from 'vitest'
import { getProfileSignals, getUvOverview } from './formulaInterpretation.js'

describe('getProfileSignals', () => {
  it('separates condition flags from regulatory cautions', () => {
    const ingredients = [
      { inciName: 'AVOID INGREDIENT', conditionFlags: [{ flagType: 'Avoid' }] },
      { inciName: 'BENEFICIAL INGREDIENT', conditionFlags: [{ flagType: 'Beneficial' }] },
      { inciName: 'RESTRICTED INGREDIENT', category: 'Restricted Substance' },
      { inciName: 'ORDINARY INGREDIENT', safetyRating: 'Green' }
    ]

    const signals = getProfileSignals(ingredients)

    expect(signals.avoid.map((item) => item.inciName)).toEqual(['AVOID INGREDIENT'])
    expect(signals.beneficial.map((item) => item.inciName)).toEqual(['BENEFICIAL INGREDIENT'])
    expect(signals.regulatory.map((item) => item.inciName)).toEqual(['RESTRICTED INGREDIENT'])
  })
})

describe('getUvOverview', () => {
  it('summarises confirmed filter types without inventing one', () => {
    const overview = getUvOverview([
      { inciName: 'FILTER A', isUvFilter: true, uvFilterType: 'Mineral/inorganic' },
      { inciName: 'FILTER B', isUvFilter: true, uvFilterType: 'Organic' },
      { inciName: 'NOT A FILTER', isUvFilter: false, uvFilterType: '' }
    ])

    expect(overview.filters.map((item) => item.inciName)).toEqual(['FILTER A', 'FILTER B'])
    expect(overview.systemType).toBe('Combination')
  })
})
