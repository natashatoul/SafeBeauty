// Cosmetic labels commonly separate ingredients with commas, bullets,
// semicolons, line breaks or a full stop followed by the next ingredient.
// A slash is deliberately not a separator:
// it can be part of a valid INCI name such as ACRYLATES/C10-30 ALKYL
// ACRYLATE CROSSPOLYMER, or join translated synonyms on a label.
export const parseIngredientList = (text = '') => text
  // Protect the dots in the packaging reference F.I.L. before treating dots
  // as list separators. The marker is restored immediately after splitting.
  .replace(/F\.I\.L\./gi, 'F\u2024I\u2024L\u2024')
  .split(/[,•;\r\n]+|\.\s+(?=[A-Za-z])/)
  .map((ingredient) => ingredient.replaceAll('\u2024', '.'))
  .map((ingredient) => ingredient.trim())
  .filter(Boolean)

export const hasLikelyMissingIngredientSeparators = (text = '', parsed = []) =>
  parsed.length <= 2
  && text.length >= 120
  && text.trim().split(/\s+/).length >= 12
