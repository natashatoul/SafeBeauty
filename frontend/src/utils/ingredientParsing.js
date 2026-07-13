// Cosmetic labels commonly separate ingredients with commas, bullets,
// semicolons or line breaks. A slash is deliberately not a separator:
// it can be part of a valid INCI name such as ACRYLATES/C10-30 ALKYL
// ACRYLATE CROSSPOLYMER, or join translated synonyms on a label.
export const parseIngredientList = (text = '') => text
  .split(/[,•;\r\n]+/)
  .map((ingredient) => ingredient.trim())
  .filter(Boolean)
