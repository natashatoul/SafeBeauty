import { describe, it, expect } from 'vitest'
import { getFormulaGroups } from './formulaInterpretation.js'

const ingredient = (inciName, functions, category = '') => ({
  inciName,
  function: functions,
  category
})

describe('getFormulaGroups', () => {
  it('assigns each ingredient to one primary practical role', () => {
    const groups = getFormulaGroups([
      ingredient('GLYCERIN', 'HUMECTANT, SKIN CONDITIONING - EMOLLIENT', 'Humectants, Emollients')
    ])

    expect(groups.length).toBe(1)
    expect(groups[0].id).toBe('moisture')
    expect(groups[0].ingredients.map((item) => item.inciName)).toEqual(['GLYCERIN'])
  })

  it('prefers texture over a secondary cleansing function', () => {
    const groups = getFormulaGroups([
      ingredient(
        'XANTHAN GUM',
        'EMULSION STABILISING, GEL FORMING, SURFACTANT - CLEANSING, VISCOSITY CONTROLLING'
      ),
      ingredient(
        'TRIETHANOLAMINE',
        'BUFFERING, SURFACTANT - CLEANSING, SURFACTANT - EMULSIFYING'
      )
    ])

    expect(groups.length).toBe(1)
    expect(groups[0].id).toBe('texture')
    expect(groups[0].ingredients.map((item) => item.inciName)).toEqual(['XANTHAN GUM', 'TRIETHANOLAMINE'])
  })
})
