import test from 'node:test'
import assert from 'node:assert/strict'
import {
  hasLikelyMissingIngredientSeparators,
  parseIngredientList
} from './ingredientParsing.js'

test('splits ingredient lists separated by full stops', () => {
  assert.deepEqual(
    parseIngredientList('Zinc Oxide [Nano]. Avene Thermal Spring Water. Titanium Dioxide [Nano].'),
    ['Zinc Oxide [Nano]', 'Avene Thermal Spring Water', 'Titanium Dioxide [Nano].']
  )
})

test('does not split dots inside an F.I.L. formula reference', () => {
  assert.deepEqual(
    parseIngredientList('Aqua. Xanthan Gum (F.I.L. N70032039/1).'),
    ['Aqua', 'Xanthan Gum (F.I.L. N70032039/1).']
  )
})

test('detects a long list whose original separators were lost', () => {
  const text = 'AQUA / WATER ALCOHOL DENAT. ' + 'GLYCERIN '.repeat(20)
  const parsed = parseIngredientList(text)

  assert.equal(hasLikelyMissingIngredientSeparators(text, parsed), true)
})
