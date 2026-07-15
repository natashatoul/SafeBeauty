// Cosmetic labels commonly separate ingredients with commas, bullets,
// middle dots, semicolons, line breaks or a full stop followed by the next
// ingredient.
// A slash is deliberately not a separator:
// it can be part of a valid INCI name such as ACRYLATES/C10-30 ALKYL
// ACRYLATE CROSSPOLYMER, or join translated synonyms on a label.
export const parseIngredientList = (text = '') => text
  // Preserve common botanical suffixes when a copied label wraps a line inside
  // a single ingredient, for example "Simmondsia Chinensis Seed\nOil".
  .replace(/\b(SEED|FRUIT|KERNEL|LEAF|ROOT|FLOWER|STEM)\s*[\r\n]+\s*(OIL|EXTRACT|WATER|WAX|BUTTER)\b/gi, '$1 $2')
  // Protect the dots in the packaging reference F.I.L. before treating dots
  // as list separators. The marker is restored immediately after splitting.
  .replace(/F\.I\.L\./gi, 'F\u2024I\u2024L\u2024')
  // Protect numeric commas inside valid chemical names such as 1,2-Hexanediol.
  .replace(/(\d),(\d)/g, '$1\u2063$2')
  .split(/[,•･・·;\r\n]+|\.\s+(?=[A-Za-z])/)
  .map((ingredient) => ingredient.replaceAll('\u2024', '.'))
  .map((ingredient) => ingredient.replaceAll('\u2063', ','))
  .map((ingredient) => ingredient.trim())
  .filter(Boolean)

export const hasLikelyMissingIngredientSeparators = (text = '', parsed = []) =>
  parsed.length <= 2
  && text.length >= 120
  && text.trim().split(/\s+/).length >= 12
