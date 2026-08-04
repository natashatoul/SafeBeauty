import { useAuth } from '../context/AuthContext'
import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useProfile } from '../context/ProfileContext'
import { analyseIngredients } from '../services/ingredientService'
import { updateAnalysis } from '../services/scanHistoryService'
import privacyIcon from '../assets/Privacy.svg'

const SKIN_TYPES = ['Normal', 'Dry', 'Oily', 'Combination', 'Sensitive']
const HAIR_CONDITIONS = ['Normal', 'Dry', 'Oily', 'Damaged', 'Colour-treated']
const AGE_GROUPS = ['Under 18', '18-25', '26-35', '36-45', '46-60', '60+']
const GENDERS = ['Female', 'Male', 'Prefer not to say']

// The value is sent to the API; the label is the readable text shown in the UI.
// Only conditions supported by the backend rule set are offered here.
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
  const { profile, saveProfile } = useProfile()
  const { isAuthenticated, logout } = useAuth()

  const location = useLocation()
  const navigate = useNavigate()
  const returnToResults = location.state?.returnToResults

  // The form is a draft copy. The shared profile changes only after Save.
  const [form, setForm] = useState(profile)
  const [saved, setSaved] = useState(false)
  const [updatingAnalysis, setUpdatingAnalysis] = useState(false)

  const selectSingleValue = (field, value) => {
    setForm((currentForm) => ({ ...currentForm, [field]: value }))
    setSaved(false)
  }

  const handleConditionToggle = (value) => {
    setForm((currentForm) => {
      const conditions = currentForm.conditions ?? []
      const updatedConditions = conditions.includes(value)
        ? conditions.filter((condition) => condition !== value)
        : [...conditions, value]

      return { ...currentForm, conditions: updatedConditions }
    })
    setSaved(false)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    await saveProfile(form)

    // When Profile was opened from Results, recalculate the same formula with
    // the new conditions and update its existing history entry.
    if (returnToResults?.results) {
      const previousResults = returnToResults.results
      const ingredients = [
        ...(previousResults.results ?? []).map((ingredient) => ingredient.inciName),
        ...(previousResults.unknownIngredients ?? []).map((ingredient) => ingredient.name)
      ]

      setUpdatingAnalysis(true)
      try {
        const results = await analyseIngredients(ingredients, form.conditions ?? [], {
          ageGroup: form.ageGroup,
          gender: form.gender
        })
        const analysisContext = {
          ...(returnToResults.analysisContext ?? {}),
          selectedConditions: form.conditions ?? [],
          ageGroup: form.ageGroup,
          gender: form.gender
        }

        if (analysisContext.historyId) {
          await updateAnalysis(analysisContext.historyId, { results, analysisContext })
        }

        navigate('/results', {
          replace: true,
          state: { results, analysisContext }
        })
        return
      } catch {
        alert('Your profile was saved, but the analysis could not be updated. Please try the analysis again.')
      } finally {
        setUpdatingAnalysis(false)
      }
    }

    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  return (
    <div className="profile-page">
      <header className="profile-page-header">
        <div>
          <h1>Your profile</h1>
          <p>Save your skin and hair details, and choose conditions that personalise ingredient warnings.</p>
        </div>
      </header>

      {isAuthenticated ? (
  <>
    <section className="profile-account-section">
      <p>You're logged in — your profile can sync across devices.</p>
      <button type="button" className="secondary-button" onClick={logout}>
        Log out
      </button>
    </section>




      <form className="profile-form" onSubmit={handleSubmit}>
        <fieldset className="profile-fieldset">
          <legend>Skin type</legend>
          <div className="profile-choice-list">
            {SKIN_TYPES.map((skinType) => (
              <label className="profile-choice" key={skinType}>
                <input
                  type="radio"
                  name="skinType"
                  value={skinType}
                  checked={form.skinType === skinType}
                  onChange={() => selectSingleValue('skinType', skinType)}
                />
                <span>{skinType}</span>
              </label>
            ))}
          </div>
        </fieldset>

        <fieldset className="profile-fieldset">
          <legend>Hair condition</legend>
          <div className="profile-choice-list">
            {HAIR_CONDITIONS.map((hairCondition) => (
              <label className="profile-choice" key={hairCondition}>
                <input
                  type="radio"
                  name="hairCondition"
                  value={hairCondition}
                  checked={form.hairCondition === hairCondition}
                  onChange={() => selectSingleValue('hairCondition', hairCondition)}
                />
                <span>{hairCondition}</span>
              </label>
            ))}
          </div>
        </fieldset>

        <div className="profile-demographic-grid">
          <fieldset className="profile-fieldset">
            <legend>Age group</legend>
            <div className="profile-choice-list">
              {AGE_GROUPS.map((ageGroup) => (
                <label className="profile-choice" key={ageGroup}>
                  <input
                    type="radio"
                    name="ageGroup"
                    value={ageGroup}
                    checked={form.ageGroup === ageGroup}
                    onChange={() => selectSingleValue('ageGroup', ageGroup)}
                  />
                  <span>{ageGroup}</span>
                </label>
              ))}
            </div>
          </fieldset>

          <fieldset className="profile-fieldset">
            <legend>Gender</legend>
            <div className="profile-choice-list">
              {GENDERS.map((gender) => (
                <label className="profile-choice" key={gender}>
                  <input
                    type="radio"
                    name="gender"
                    value={gender}
                    checked={form.gender === gender}
                    onChange={() => selectSingleValue('gender', gender)}
                  />
                  <span>{gender}</span>
                </label>
              ))}
            </div>
          </fieldset>
        </div>

        <fieldset className="profile-fieldset profile-conditions-fieldset">
          <legend>Conditions</legend>
          <p>Select only conditions you want the ingredient analysis to consider.</p>
          <div className="profile-condition-grid">
            {CONDITIONS.map((condition) => {
              const selected = (form.conditions ?? []).includes(condition.value)

              return (
                <label
                  className={`profile-condition-option${selected ? ' selected' : ''}`}
                  key={condition.value}
                >
                  <input
                    type="checkbox"
                    checked={selected}
                    onChange={() => handleConditionToggle(condition.value)}
                  />
                  <span className="profile-checkbox" aria-hidden="true">
                    {selected ? '✓' : ''}
                  </span>
                  <span>{condition.label}</span>
                </label>
              )
            })}
          </div>
        </fieldset>

        {/* aside is used for supporting information related to the form.
    This privacy note explains how profile data is stored, but it is not
    one of the editable profile fields. Semantically, aside describes
    this purpose more clearly than a generic div. */}
        <aside className="profile-privacy-note">
          <img src={privacyIcon} alt="" aria-hidden="true" />
          <p>
            Your profile is stored only in this browser. Selected conditions are sent with an
            analysis request solely to personalise the ingredient guidance.
          </p>
        </aside>

        <div className="profile-actions">
          <button type="submit" className="primary-button" disabled={updatingAnalysis}>
            {updatingAnalysis ? 'Saving profile...' : 'Save profile'}
          </button>
          {saved && <span className="profile-saved-message" role="status">✓ Profile saved</span>}
        </div>
            </form>
      </>
    ) : (
      <section className="profile-account-section">
        <p>Log in to set up your skin, hair and condition preferences and sync them across devices.</p>
        <div className="profile-account-actions">
          <button type="button" className="primary-button" onClick={() => navigate('/login')}>
            Log in
          </button>
          <button type="button" className="secondary-button" onClick={() => navigate('/register')}>
            Create account
          </button>
        </div>
      </section>
    )}

    </div>
  )
}

export default ProfilePage
