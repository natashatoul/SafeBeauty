import { describe, it, expect } from 'vitest'
import { parseIngredientList, hasLikelyMissingIngredientSeparators } from './ingredientParsing.js'



describe('splitsIngredientList', () => {
    it('splits ingredient lists separated by full stops', () =>
        expect(
            parseIngredientList('Zinc Oxide [Nano]. Avene Thermal Spring Water. Titanium Dioxide [Nano].')
        ).toEqual(
            ['Zinc Oxide [Nano]', 'Avene Thermal Spring Water', 'Titanium Dioxide [Nano].']
        )
    )

    it('does not split dots inside an F.I.L. formula reference', () =>
    expect(
        parseIngredientList('Aqua. Xanthan Gum (F.I.L. N70032039/1).')
    ).toEqual(
    ['Aqua', 'Xanthan Gum (F.I.L. N70032039/1).']
    )
    )

    it('splits ingredient lists separated by middle dots', () =>
    expect(
        parseIngredientList('DIMETHICONE･WATER(AQUA/EAU)･DIISOPROPYL SEBACATE')
    ).toEqual(
       ['DIMETHICONE', 'WATER(AQUA/EAU)', 'DIISOPROPYL SEBACATE'] 
    )
    )

    it('preserves numeric commas inside chemical names', () =>
    expect(
      parseIngredientList('Oil,1,2-Hexanediol,Tocopherol'),  
    ).toEqual(
        ['Oil', '1,2-Hexanediol', 'Tocopherol']
    ))

    it('preserves botanical suffixes split by a copied line break', () =>
    expect(
        parseIngredientList('Simmondsia Chinensis (Jojoba) Seed\nOil,1,2-Hexanediol'),
    ).toEqual(
        ['Simmondsia Chinensis (Jojoba) Seed Oil', '1,2-Hexanediol']
    ))
})

describe('hasLikelyMissingIngredientSeparators', () => {
  it('detects a long list whose original separators were lost', () => {
    const text = 'AQUA / WATER ALCOHOL DENAT. ' + 'GLYCERIN '.repeat(20)
    const parsed = parseIngredientList(text)

    expect(hasLikelyMissingIngredientSeparators(text, parsed)).toBe(true)
  })
})

