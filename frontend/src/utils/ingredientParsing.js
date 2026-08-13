// functions are use in tests and in ManualInputPage.jsx

// Patterns used to clean and split cosmetic ingredient lists.

const PLANT_INGREDIENT_PATTERN =
  /\b(SEED|FRUIT|KERNEL|LEAF|ROOT|FLOWER|STEM)\s*[\r\n]+\s*(OIL|EXTRACT|WATER|WAX|BUTTER)\b/gi;

const FIL_PATTERN =
  /F\.\s*I\.\s*L\./gi;

const DECIMAL_COMMA_PATTERN =
  /(\d),(\d)/g;

const INGREDIENT_SEPARATOR_PATTERN =
  /[,•･・·;\r\n]+|\.\s+(?=[A-Za-z])/;


export const parseIngredientList = (text = '') => {
  const normalisedText = text

    // Join ingredient names broken across lines.
    .replace(
      PLANT_INGREDIENT_PATTERN,
      '$1 $2'
    )

    // \u2024 — "ONE DOT LEADER" small dot like normal. , but differ
    // \u2063 — "INVISIBLE SEPARATOR"
    .replace(
      FIL_PATTERN,
      'F\u2024I\u2024L\u2024'
    )

    // Protect decimal commas such as "1,5%".
    .replace(
      DECIMAL_COMMA_PATTERN,
      '$1\u2063$2'
    );

  return normalisedText
    // Split the ingredient list at common separators.
    .split(INGREDIENT_SEPARATOR_PATTERN)

    // Restore protected characters.
    .map((ingredient) =>
      ingredient.replaceAll('\u2024', '.')
    )
    .map((ingredient) =>
      ingredient.replaceAll('\u2063', ',')
    )

    // Clean each ingredient.
    .map((ingredient) => ingredient.trim())
    .filter(Boolean);
};


export const hasLikelyMissingIngredientSeparators = (
  text = '',
  parsed = []
) => {
  const wordCount = text.trim().split(/\s+/).length;

  // A long text producing only one or two ingredients
  // may indicate that ingredient separators were lost.
  return (
    parsed.length <= 2 &&
    text.length >= 120 &&
    wordCount >= 12
  );
};
