import { createContext, useContext, useState } from 'react'

// Creates a "channel" that components can subscribe to.
// On its own it doesn't hold any data yet — it's just the wiring
// that lets data travel from one place to many components at once,
// without passing it down manually through every level in between.
const ProfileContext = createContext()

const EMPTY_PROFILE = {
  skinType: '',
  hairCondition: '',
  conditions: [],
  ageGroup: '',
  gender: ''
}

function loadSavedProfile() {
  try {
    const saved = localStorage.getItem('safebeauty_profile')
    if (!saved) return EMPTY_PROFILE

    const parsed = JSON.parse(saved)
    return {
      ...EMPTY_PROFILE,
      ...parsed,
      conditions: Array.isArray(parsed.conditions) ? parsed.conditions : []
    }
  } catch {
    return EMPTY_PROFILE
  }
}

// This component is a "wrapper" that will sit near the top of the app.
// `children` is a special prop meaning "whatever is nested inside this component"
// e.g. <ProfileProvider><App /></ProfileProvider> makes <App /> the children here.
export function ProfileProvider({ children }) {
  // The actual profile data, stored as one object with several fields.
  // Starts empty/default until either localStorage or the user fills it in.
  const [profile, setProfile] = useState(loadSavedProfile)

  // Function used whenever the profile needs to be updated
  // (e.g. when the user fills in or edits their profile form).
  // It does two things every time it's called:
  const saveProfile = (newProfile) => {
    // 1) Updates React's state, so the UI immediately reflects the new data.
    setProfile(newProfile)

    // 2) Persists the same data to localStorage, so it isn't lost
    // if the user reloads the page or closes the browser.
    // JSON.stringify() is the reverse of JSON.parse() — it turns
    // a JS object into a string, since that's the only format
    // localStorage accepts.
    localStorage.setItem('safebeauty_profile', JSON.stringify(newProfile))
  }

  // The Provider is what actually broadcasts the data.
  // Anything placed in `value` becomes available to every component
  // nested inside {children}, no matter how deeply nested it is —
  // no need to pass profile/saveProfile down manually through props.
  return (
    <ProfileContext.Provider value={{ profile, saveProfile }}>
      {children}
    </ProfileContext.Provider>
  )
}

// A small custom hook that wraps useContext(ProfileContext).
// This means other files only need to import `useProfile` —
// they don't need to know about `useContext` or `ProfileContext` directly.
// Usage elsewhere in the app: const { profile, saveProfile } = useProfile()
export function useProfile() {
  return useContext(ProfileContext)
}
