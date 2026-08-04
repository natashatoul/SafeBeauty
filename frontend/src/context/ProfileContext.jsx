import { createContext, useContext, useEffect, useState } from 'react'
import { useAuth } from './AuthContext'
import { getProfile, saveProfile as saveProfileToServer } from '../services/userProfileService'

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

// This component is a "wrapper" that will sit near the top of the app.
// `children` is a special prop meaning "whatever is nested inside this component"
// e.g. <ProfileProvider><App /></ProfileProvider> makes <App /> the children here.
// children it is a prop - it is what componet get from outside
export function ProfileProvider({ children }) {
  const { isAuthenticated } = useAuth()
  // it is a State - it is what component it self hold and change
  // The actual profile data, stored as one object with several fields.
  // Starts empty until the useEffect below loads it from the server.
  const [profile, setProfile] = useState(EMPTY_PROFILE)

  useEffect(() => {
    if (!isAuthenticated) {
      setProfile(EMPTY_PROFILE)
      return
    }

    getProfile().then((loaded) => {
      if (loaded) setProfile(loaded)
    })
  }, [isAuthenticated])

  const saveProfile = async (newProfile) => {
    setProfile(newProfile)
    await saveProfileToServer(newProfile)
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
