const STORAGE_KEY = 'safebeauty_analysis_history_v1'
const MAX_HISTORY_ITEMS = 20

// localStorage stores only strings, so every history operation passes through
// JSON.stringify/JSON.parse. Keeping that detail in one utility prevents each
// page from implementing storage and error handling in a slightly different way.
const getStorage = () => globalThis.localStorage

const createId = () => globalThis.crypto?.randomUUID?.()
  ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`

// createID can generate unique numbers in two methods
// cripto is a function randomUUID() which can produce unique numbers
// this function inside browser
// function createId() {
//   if (
//     globalThis.crypto &&
//     typeof globalThis.crypto.randomUUID === 'function'
//   ) {
//     return globalThis.crypto.randomUUID()
//   }

//   const currentTime = Date.now()
//   const randomNumber = Math.random()
//   const hexadecimalNumber = randomNumber.toString(16)
//   const randomPart = hexadecimalNumber.slice(2)

//   return `${currentTime}-${randomPart}`
// }

const isHistoryEntry = (entry) => entry
  && typeof entry === 'object'
  && typeof entry.id === 'string'
  && typeof entry.createdAt === 'string'
  && entry.results
  && typeof entry.results === 'object'
  && entry.analysisContext
  && typeof entry.analysisContext === 'object'

const writeHistory = (history) => {
  getStorage().setItem(STORAGE_KEY, JSON.stringify(history))
}

export const getHistory = () => {
  try {
    const rawHistory = getStorage().getItem(STORAGE_KEY)
    if (!rawHistory) return []

    const parsedHistory = JSON.parse(rawHistory)
    if (!Array.isArray(parsedHistory)) return []

    return parsedHistory.filter(isHistoryEntry).slice(0, MAX_HISTORY_ITEMS)
  } catch {
    // Storage may be unavailable, full, or contain malformed JSON. History is
    // optional, so a storage problem must never prevent a product analysis.
    return []
  }
}

export const getAnalysis = (id) =>
  getHistory().find((entry) => entry.id === id) ?? null

export const saveAnalysis = ({ results, analysisContext = {} }) => {
  try {
    const id = createId()
    const entry = {
      id,
      createdAt: new Date().toISOString(),
      results,
      analysisContext: {
        ...analysisContext,
        historyId: id
      }
    }

    writeHistory([entry, ...getHistory()].slice(0, MAX_HISTORY_ITEMS))
    return entry
  } catch {
    return null
  }
}

export const updateAnalysis = (id, { results, analysisContext = {} }) => {
  try {
    const history = getHistory()
    const index = history.findIndex((entry) => entry.id === id)
    if (index === -1) return null

    const existingEntry = history[index]
    const updatedEntry = {
      ...existingEntry,
      results,
      analysisContext: {
        ...existingEntry.analysisContext,
        ...analysisContext,
        historyId: id
      },
      updatedAt: new Date().toISOString()
    }

    history[index] = updatedEntry
    writeHistory(history)
    return updatedEntry
  } catch {
    return null
  }
}

export const removeAnalysis = (id) => {
  try {
    const history = getHistory()
    const updatedHistory = history.filter((entry) => entry.id !== id)
    if (updatedHistory.length === history.length) return false

    writeHistory(updatedHistory)
    return true
  } catch {
    return false
  }
}

export const clearHistory = () => {
  try {
    getStorage().removeItem(STORAGE_KEY)
    return true
  } catch {
    return false
  }
}

