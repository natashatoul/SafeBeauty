import { createContext, useContext, useState, useEffect } from 'react'

// Creates a "channel" that components can subscribe to.
// On its own it doesn't hold any data yet — it's just the wiring
// that lets data travel from one place to many components at once,
// without passing it down manually through every level in between.
const ProfileContext = createContext()

// This component is a "wrapper" that will sit near the top of the app.
// `children` is a special prop meaning "whatever is nested inside this component"
// e.g. <ProfileProvider><App /></ProfileProvider> makes <App /> the children here.
export function ProfileProvider({ children }) {
  // The actual profile data, stored as one object with several fields.
  // Starts empty/default until either localStorage or the user fills it in.
  const [profile, setProfile] = useState({
    skinType: '',
    hairCondition: '',
    conditions: [],
    ageGroup: '',
    gender: ''
  })

  // HOOK: useEffect()
  // Runs side effects — things outside the normal "get data, show data" flow,
  // like reading from localStorage, calling an API, or setting up a timer.
  // The empty array [] as the second argument means:
  // "run this code exactly once, right after the component first renders,
  // and never again after that."
  useEffect(() => {
    // localStorage is the browser's built-in storage that survives
    // page reloads and closing the tab — unlike useState, which resets
    // every time the page reloads.
    // getItem() looks up whatever was saved under this key.
    // Returns null if nothing was ever saved.
    const saved = localStorage.getItem('safebeauty_profile')

    // localStorage can only store strings, never objects directly.
    // So saved data is stored as a JSON string, and JSON.parse()
    // converts that string back into a real JS object we can use.
    // The `if (saved)` check avoids trying to parse `null`
    // (which would happen on someone's very first visit).
    if (saved) setProfile(JSON.parse(saved))
  }, [])

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