import test from 'node:test'
import assert from 'node:assert/strict'
import { getFormulaGroups } from './formulaInterpretation.js'

const ingredient = (inciName, functions, category = '') => ({
  inciName,
  function: functions,
  category
})

test('assigns each ingredient to one primary practical role', () => {
  const groups = getFormulaGroups([
    ingredient('GLYCERIN', 'HUMECTANT, SKIN CONDITIONING - EMOLLIENT', 'Humectants, Emollients')
  ])

  assert.equal(groups.length, 1)
  assert.equal(groups[0].id, 'moisture')
  assert.deepEqual(groups[0].ingredients.map((item) => item.inciName), ['GLYCERIN'])
})

test('prefers texture over a secondary cleansing function', () => {
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

  assert.equal(groups.length, 1)
  assert.equal(groups[0].id, 'texture')
  assert.deepEqual(
    groups[0].ingredients.map((item) => item.inciName),
    ['XANTHAN GUM', 'TRIETHANOLAMINE']
  )
})
