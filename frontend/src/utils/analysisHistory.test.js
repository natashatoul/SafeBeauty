import test, { after, beforeEach } from 'node:test'
import assert from 'node:assert/strict'
import {
  clearHistory,
  getAnalysis,
  getHistory,
  removeAnalysis,
  saveAnalysis,
  updateAnalysis
} from './analysisHistory.js'

const originalLocalStorage = globalThis.localStorage

const createMemoryStorage = () => {
  const values = new Map()

  return {
    getItem: (key) => values.has(key) ? values.get(key) : null,
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
    clear: () => values.clear()
  }
}

const analysis = (name) => ({
  results: [{ inciName: name, safetyRating: 'Grey' }],
  unknownIngredients: [],
  aiSummary: `${name} summary`
})

beforeEach(() => {
  globalThis.localStorage = createMemoryStorage()
})

after(() => {
  globalThis.localStorage = originalLocalStorage
})

test('saves and reads a complete analysis with the same history id in its context', () => {
  const saved = saveAnalysis({
    results: analysis('GLYCERIN'),
    analysisContext: { source: 'Manual input' }
  })

  assert.ok(saved)
  assert.equal(saved.analysisContext.historyId, saved.id)
  assert.deepEqual(getAnalysis(saved.id), saved)
})

test('keeps the newest 20 analyses', () => {
  for (let index = 1; index <= 21; index += 1) {
    saveAnalysis({
      results: analysis(`INGREDIENT ${index}`),
      analysisContext: { source: 'Manual input' }
    })
  }

  const history = getHistory()
  assert.equal(history.length, 20)
  assert.equal(history[0].results.results[0].inciName, 'INGREDIENT 21')
  assert.equal(history.at(-1).results.results[0].inciName, 'INGREDIENT 2')
})

test('updates an analysis without changing its id or creation date', () => {
  const saved = saveAnalysis({
    results: analysis('PARFUM'),
    analysisContext: { source: 'Manual input', selectedConditions: [] }
  })

  const updated = updateAnalysis(saved.id, {
    results: analysis('PARFUM UPDATED'),
    analysisContext: { selectedConditions: ['AtopicDermatitis'] }
  })

  assert.equal(updated.id, saved.id)
  assert.equal(updated.createdAt, saved.createdAt)
  assert.equal(updated.analysisContext.historyId, saved.id)
  assert.deepEqual(updated.analysisContext.selectedConditions, ['AtopicDermatitis'])
  assert.ok(updated.updatedAt)
})

test('removes one analysis and clears all history', () => {
  const first = saveAnalysis({ results: analysis('FIRST'), analysisContext: {} })
  const second = saveAnalysis({ results: analysis('SECOND'), analysisContext: {} })

  assert.equal(removeAnalysis(first.id), true)
  assert.equal(getAnalysis(first.id), null)
  assert.equal(getHistory().length, 1)
  assert.equal(getHistory()[0].id, second.id)

  assert.equal(clearHistory(), true)
  assert.deepEqual(getHistory(), [])
})

test('returns an empty history when stored JSON is corrupted', () => {
  globalThis.localStorage.setItem('safebeauty_analysis_history_v1', '{broken json')
  assert.deepEqual(getHistory(), [])
})
