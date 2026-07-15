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

test('splits ingredient lists separated by middle dots', () => {
  assert.deepEqual(
    parseIngredientList('DIMETHICONE･WATER(AQUA/EAU)･DIISOPROPYL SEBACATE'),
    ['DIMETHICONE', 'WATER(AQUA/EAU)', 'DIISOPROPYL SEBACATE']
  )
})

test('preserves numeric commas inside chemical names', () => {
  assert.deepEqual(
    parseIngredientList('Oil,1,2-Hexanediol,Tocopherol'),
    ['Oil', '1,2-Hexanediol', 'Tocopherol']
  )
})

test('preserves botanical suffixes split by a copied line break', () => {
  assert.deepEqual(
    parseIngredientList('Simmondsia Chinensis (Jojoba) Seed\nOil,1,2-Hexanediol'),
    ['Simmondsia Chinensis (Jojoba) Seed Oil', '1,2-Hexanediol']
  )
})

test('detects a long list whose original separators were lost', () => {
  const text = 'AQUA / WATER ALCOHOL DENAT. ' + 'GLYCERIN '.repeat(20)
  const parsed = parseIngredientList(text)

  assert.equal(hasLikelyMissingIngredientSeparators(text, parsed), true)
})
