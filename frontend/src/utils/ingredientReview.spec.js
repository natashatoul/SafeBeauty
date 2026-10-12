import { describe, expect, it } from 'vitest'
import { getIngredientReviewGroups } from './ingredientReview.js'

describe('getIngredientReviewGroups', () => {
  it('uses input order to connect previous results to the original entries', () => {
    const sourceEntries = ['Aqua', 'Broken ingredient', 'Glycerin']
    const analysis = {
      results: [
        { inciName: 'AQUA', inputOrder: 0 },
        { inciName: 'GLYCERIN', inputOrder: 2 }
      ],
      unknownIngredients: [
        { name: 'BROKEN INGREDIENT', inputOrder: 1 }
      ]
    }

    expect(getIngredientReviewGroups(sourceEntries, analysis)).toEqual({
      matched: ['Aqua', 'Glycerin'],
      needsChecking: ['Broken ingredient']
    })
  })

  it('falls back to result names for older results without input order', () => {
    const analysis = {
      results: [{ inciName: 'AQUA' }],
      unknownIngredients: [{ name: 'UNKNOWN EXTRACT' }]
    }

    expect(getIngredientReviewGroups([], analysis)).toEqual({
      matched: ['AQUA'],
      needsChecking: ['UNKNOWN EXTRACT']
    })
  })
})
