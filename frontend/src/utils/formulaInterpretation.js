const splitValues = (text = '') => text
  .split(',')
  .map((value) => value.trim())
  .filter(Boolean)

export const hasCategory = (ingredient, category) =>
  splitValues(ingredient.category).includes(category)

export const hasFunction = (ingredient, functionName) =>
  splitValues(ingredient.function).includes(functionName)

const FORMULA_GROUPS = [
  {
    id: 'moisture',
    title: 'Moisture support',
    technical: 'Humectants',
    description: 'helps attract and hold water in the skin.',
    matches: (item) => hasCategory(item, 'Humectants') || hasFunction(item, 'HUMECTANT')
  },
  {
    id: 'softening',
    title: 'Softening and moisture barrier',
    technical: 'Emollients and skin protectants',
    description: 'helps soften the skin and reduce moisture loss.',
    matches: (item) => hasCategory(item, 'Emollients')
      || hasFunction(item, 'SKIN CONDITIONING - EMOLLIENT')
      || hasFunction(item, 'SKIN PROTECTING')
  },
  {
    id: 'texture',
    title: 'Texture and stability',
    technical: 'Emulsifiers, thickeners and stabilisers',
    description: 'keeps the formula mixed and gives it thickness and spreadability.',
    matches: (item) => [
      'EMULSION STABILISING',
      'SURFACTANT - EMULSIFYING',
      'VISCOSITY CONTROLLING',
      'GEL FORMING',
      'OPACIFYING'
    ].some((name) => hasFunction(item, name))
  },
  {
    id: 'preservation',
    title: 'Product protection',
    technical: 'Preservatives',
    description: 'helps limit microbial growth and keep the product usable after opening.',
    matches: (item) => hasCategory(item, 'Preservative')
  },
  {
    id: 'cleansing',
    title: 'Cleansing system',
    technical: 'Cleansing surfactants',
    description: 'helps lift oils or residue so they can be rinsed away.',
    matches: (item) => hasFunction(item, 'SURFACTANT - CLEANSING')
      || hasFunction(item, 'CLEANSING')
  },
  {
    id: 'fragrance',
    title: 'Fragrance',
    technical: 'Perfuming ingredients',
    description: 'adds scent and may matter for fragrance-sensitive users.',
    matches: (item) => hasCategory(item, 'Fragrance')
  },
  {
    id: 'buffering',
    title: 'pH balance',
    technical: 'Buffering agents',
    description: 'helps keep the product\'s pH stable during storage and use.',
    matches: (item) => hasFunction(item, 'BUFFERING')
  }

]

const PRIMARY_ROLE_PRIORITY = [
  'fragrance',
  'preservation',
  'moisture',
  'softening',
  'texture',
  'cleansing',
  'buffering'
]

export const getFormulaGroups = (ingredients) => {
  const ingredientsByGroup = new Map(FORMULA_GROUPS.map((group) => [group.id, []]))
  const groupsByPriority = PRIMARY_ROLE_PRIORITY
    .map((id) => FORMULA_GROUPS.find((group) => group.id === id))
    .filter(Boolean)

  ingredients.forEach((ingredient) => {
    const primaryGroup = groupsByPriority.find((group) => group.matches(ingredient))
    if (primaryGroup) ingredientsByGroup.get(primaryGroup.id).push(ingredient)
  })

  return FORMULA_GROUPS
    .map((group) => ({
      ...group,
      ingredients: ingredientsByGroup.get(group.id)
    }))
    .filter((group) => group.ingredients.length > 0)
}

export const getProfileSignals = (ingredients) => {
  const avoid = ingredients.filter((item) =>
    item.conditionFlags?.some((flag) => flag.flagType === 'Avoid'))
  const beneficial = ingredients.filter((item) =>
    item.conditionFlags?.some((flag) => flag.flagType === 'Beneficial'))
  const regulatory = ingredients.filter((item) =>
    item.safetyRating === 'Red'
    || item.safetyRating === 'Amber'
    || hasCategory(item, 'Restricted Substance'))

  return { avoid, beneficial, regulatory }
}

export const getUvOverview = (ingredients) => {
  const filters = ingredients.filter((item) => item.isUvFilter)
  const types = [...new Set(filters.map((item) => item.uvFilterType).filter(Boolean))]

  return {
    filters,
    systemType: types.length > 1 ? 'Combination' : types[0] || ''
  }
}
