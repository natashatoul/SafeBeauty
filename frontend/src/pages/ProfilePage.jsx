import { useAuth } from '../context/AuthContext'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useProfile } from '../context/ProfileContext'
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

  const navigate = useNavigate()


  // The form is a draft copy. The shared profile changes only after Save.
  const [form, setForm] = useState(profile)
  const [saved, setSaved] = useState(false)
  const [saveError, setSaveError] = useState(false)


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
    const success = await saveProfile(form)

    if (success) {
      setSaved(true)
      setSaveError(false)
      setTimeout(() => setSaved(false), 2000)
    } else {
      setSaveError(true)
    }
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
                <label className="profile-choice" key="skinType-not-specified">
                  <input
                    type="radio"
                    name="skinType"
                    value=""
                    checked={form.skinType === ''}
                    onChange={() => selectSingleValue('skinType', '')}
                  />
                  <span>Not specified</span>
                </label>
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
                <label className="profile-choice" key="hairCondition-not-specified">
                  <input
                    type="radio"
                    name="hairCondition"
                    value=""
                    checked={form.hairCondition === ''}
                    onChange={() => selectSingleValue('hairCondition', '')}
                  />
                  <span>Not specified</span>
                </label>
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
                Your profile is saved to your account. Selected conditions are used to personalise
                the ingredient guidance and are not shared with third parties.

              </p>
            </aside>

            <div className="profile-actions">
              <button type="submit" className="primary-button">
                Save profile
              </button>

              {saveError && (
                <span className="profile-save-error" role="alert">
                  Could not save your profile. Please check your connection and try again.
                </span>
              )}

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
