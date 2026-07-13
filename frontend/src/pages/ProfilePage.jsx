import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useProfile } from '../context/ProfileContext'
import { analyseIngredients } from '../services/ingredientService'

// Plain arrays of option strings for dropdowns that don't need separate
// "technical value" vs "display label" — the value shown IS the value stored.
const SKIN_TYPES = ['Normal', 'Dry', 'Oily', 'Combination', 'Sensitive']
const HAIR_CONDITIONS = ['Normal', 'Dry', 'Oily', 'Damaged', 'Colour-treated']
const AGE_GROUPS = ['Under 18', '18-25', '26-35', '36-45', '46-60', '60+']
const GENDERS = ['Female', 'Male', 'Non-binary', 'Prefer not to say']

// Array of objects instead of plain strings, because the technical name
// (value, used internally / sent to the backend) and the human-friendly
// name (label, shown to the user) are different — e.g. "AtopicDermatitis"
// vs "Eczema (Atopic Dermatitis)".
const CONDITIONS = [
  { value: 'Acne', label: 'Acne' },
  { value: 'AtopicDermatitis', label: 'Eczema (Atopic Dermatitis)' },
  { value: 'Rosacea', label: 'Rosacea' },
  { value: 'Psoriasis', label: 'Psoriasis' },
  { value: 'Alopecia', label: 'Hair Loss (Alopecia)' },
  { value: 'SeborrhoeicDermatitis', label: 'Seborrhoeic Dermatitis (Dandruff)' },
  { value: 'KeratosisPilaris', label: 'Keratosis Pilaris ("Chicken Skin")' },
  { value: 'ActinicKeratoses', label: 'Actinic Keratoses' }
]

function ProfilePage() {
  // useProfile() is the custom hook from ProfileContext.jsx.
  // It gives us the profile currently saved in Context (and, behind the
  // scenes, in localStorage), plus the function to save a new one.
  const { profile, saveProfile } = useProfile()
  const location = useLocation()
  const navigate = useNavigate()
  const returnToResults = location.state?.returnToResults

  // "form" is a SEPARATE, local piece of state — a draft copy of profile.
  // It starts out equal to profile, but from this point on it lives its
  // own life: editing the form does NOT touch the real saved profile
  // until the user explicitly submits it. This is the standard pattern
  // for editable forms — you edit a draft, then commit it on submit.
  const [form, setForm] = useState(profile)

  // A separate, independent flag: whether to currently show the
  // "✓ Saved!" confirmation message.
  const [saved, setSaved] = useState(false)
  const [updatingAnalysis, setUpdatingAnalysis] = useState(false)

  // Handles every plain dropdown (skinType, hairCondition, ageGroup, gender).
  // One function works for all four fields — see explanation below.
  const handleChange = (e) => {
    // { ...form, ... } is the SPREAD OPERATOR for objects.
    // It means "copy every existing field from form into a new object".
    // This is necessary because setForm() replaces the WHOLE object —
    // without spreading first, updating one field would wipe out all
    // the others (e.g. changing skinType would erase gender, ageGroup, etc).
    //
    // [e.target.name]: e.target.value is a COMPUTED PROPERTY NAME.
    // e.target is the actual <select> element that triggered the change.
    // e.target.name is its "name" attribute (e.g. "skinType", "gender" —
    // see the name="..." attributes in the JSX below).
    // Wrapping it in [ ] means "use the VALUE of e.target.name as the
    // field name", not the literal text "e.target.name".
    // So this one function correctly updates whichever field actually changed.
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  // Handles checkbox toggling for skin/hair conditions.
  const handleConditionToggle = (value) => {
    // .includes(value) checks: is this value already in the conditions array?
    // Ternary operator (condition ? ifTrue : ifFalse):
    const conditions = form.conditions ?? []
    const updated = conditions.includes(value)
      // Already checked -> UNCHECK: keep everything EXCEPT this value
      // (.filter() builds a new array with only the items that pass the test)
      ? conditions.filter(c => c !== value)
      // Not checked yet -> CHECK: keep everything that was there, plus this new value
      // ([...form.conditions, value] is the spread operator for ARRAYS —
      // same idea as for objects above, but building a new array instead)
      : [...conditions, value]

    // Same object-spread pattern as handleChange: keep all other fields,
    // replace only the "conditions" field with the newly computed array.
    setForm({ ...form, conditions: updated })
  }

  // Runs when the form is submitted (button click, or pressing Enter in a field).
  const handleSubmit = async (e) => {
    // By default, submitting an HTML <form> reloads the whole page —
    // old browser behaviour from before JavaScript handled this.
    // preventDefault() cancels that, so our own code below runs instead.
    e.preventDefault()

    // This is the moment the "draft" (form) becomes the real, saved profile.
    // saveProfile updates both the Context state AND localStorage
    // (see ProfileContext.jsx — that's where the actual saving logic lives).
    saveProfile(form)

    if (returnToResults?.results) {
      const previousResults = returnToResults.results
      const ingredients = [
        ...(previousResults.results ?? []).map((ingredient) => ingredient.inciName),
        ...(previousResults.unknownIngredients ?? []).map((ingredient) => ingredient.name)
      ]

      setUpdatingAnalysis(true)
      try {
        const results = await analyseIngredients(ingredients, form.conditions ?? [])
        navigate('/results', {
          replace: true,
          state: {
            results,
            analysisContext: {
              ...(returnToResults.analysisContext ?? {}),
              selectedConditions: form.conditions ?? []
            }
          }
        })
        return
      } catch {
        alert('Your profile was saved, but the analysis could not be updated. Please try the analysis again.')
      } finally {
        setUpdatingAnalysis(false)
      }
    }

    // Show the "✓ Saved!" confirmation.
    setSaved(true)

    // setTimeout runs code once, after a delay (in milliseconds).
    // 2000ms = 2 seconds. After that time, saved is set back to false,
    // so the confirmation message disappears on its own —
    // the user doesn't have to dismiss it manually.
    setTimeout(() => setSaved(false), 2000)
  }

  return (
    <div>
      <h2>My Profile</h2>
      <p>
        Your profile is stored locally in this browser. Selected conditions are sent with an analysis
        request so the ingredient rules can be personalised.
      </p>

      {/* onSubmit fires when the form is submitted (button click or Enter key) */}
      <form onSubmit={handleSubmit}>
        <div>
          <label>Skin Type</label>
          {/* Controlled select: what's shown always matches form.skinType.
              name="skinType" is what handleChange reads via e.target.name. */}
          <select name="skinType" value={form.skinType} onChange={handleChange}>
            <option value="">Select...</option>
            {/* .map() turns the plain array of strings into <option> elements */}
            {SKIN_TYPES.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>

        <div>
          <label>Hair Condition</label>
          <select name="hairCondition" value={form.hairCondition} onChange={handleChange}>
            <option value="">Select...</option>
            {HAIR_CONDITIONS.map(h => <option key={h} value={h}>{h}</option>)}
          </select>
        </div>

        <div>
          <label>Age Group</label>
          <select name="ageGroup" value={form.ageGroup} onChange={handleChange}>
            <option value="">Select...</option>
            {AGE_GROUPS.map(a => <option key={a} value={a}>{a}</option>)}
          </select>
        </div>

        <div>
          <label>Gender</label>
          <select name="gender" value={form.gender} onChange={handleChange}>
            <option value="">Select...</option>
            {GENDERS.map(g => <option key={g} value={g}>{g}</option>)}
          </select>
        </div>

        <div>
          <label>Skin & Hair Conditions</label>
          {/* .map() over an array of OBJECTS this time (value + label pairs) */}
          {CONDITIONS.map(c => (
            <div key={c.value}>
              <input
                type="checkbox"
                id={c.value}
                // checked reflects whether c.value is currently in the conditions array
                checked={(form.conditions ?? []).includes(c.value)}
                // onChange calls the toggle function with this specific checkbox's value
                onChange={() => handleConditionToggle(c.value)}
              />
              {/* htmlFor links the label to the checkbox by id — plain HTML/accessibility,
                  not React-specific — so clicking the text also toggles the checkbox */}
              <label htmlFor={c.value}>{c.label}</label>
            </div>
          ))}
        </div>

        <button type="submit" className="primary-button" disabled={updatingAnalysis}>
          {updatingAnalysis
            ? 'Updating analysis...'
            : returnToResults
              ? 'Save profile and update analysis'
              : 'Save Profile'}
        </button>
        {returnToResults && (
          <button
            type="button"
            className="secondary-button"
            onClick={() => navigate(-1)}
            disabled={updatingAnalysis}
          >
            Back to results without changes
          </button>
        )}
        {/* Conditional rendering: only show this when saved is true */}
        {saved && <span> ✓ Saved!</span>}
      </form>
    </div>
  )
}

export default ProfilePage
