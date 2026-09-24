import { useState } from 'react'
import eyeIcon from '../assets/Eye.svg'
import eyeOffIcon from '../assets/EyeOff.svg'

function PasswordInput({ id, value, onChange, autoComplete }) {
  const [isPasswordVisible, setIsPasswordVisible] = useState(false)

  return (
    <div className="password-input-wrapper">
      <input
        id={id}
        type={isPasswordVisible ? 'text' : 'password'}
        value={value}
        onChange={onChange}
        autoComplete={autoComplete}
        required
      />
      <button
        type="button"
        className="password-visibility-button"
        onClick={() => setIsPasswordVisible((isVisible) => !isVisible)}
        aria-label={isPasswordVisible ? 'Hide password' : 'Show password'}
        aria-pressed={isPasswordVisible}
      >
        <img
          src={isPasswordVisible ? eyeOffIcon : eyeIcon}
          alt=""
          aria-hidden="true"
        />
      </button>
    </div>
  )
}

export default PasswordInput
