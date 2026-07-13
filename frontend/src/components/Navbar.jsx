import { NavLink, useLocation } from 'react-router-dom'
import homeIcon from '../assets/Home.svg'
import scanIcon from '../assets/Scan.svg'
import historyIcon from '../assets/History.svg'
import profileIcon from '../assets/Profile.svg'
import desktopLogo from '../assets/Logo_desktop.svg'

function Navbar() {
  const location = useLocation()
  // Navbar is a React component.
  // navItems is an array of objects. Each object describes one navigation link:
  // where it goes, what text it shows, and which icon it uses.
  const navItems = [
    { to: '/', label: 'Home', icon: homeIcon },
    { to: '/scan', label: 'Scan', icon: scanIcon },
    { to: '/manual', label: 'Enter list', icon: historyIcon },
    { to: '/profile', label: 'Profile', icon: profileIcon }
  ]

  return (
    <aside className="sidebar">
      {/* NavLink is a React Router component.
          This one works as a clickable logo that sends the user to the home page. */}
      <NavLink to="/" className="brand" aria-label="SafeBeauty home">
        <img src={desktopLogo} alt="SafeBeauty" className="brand-logo" />
      </NavLink>

      <nav className="side-nav" aria-label="Main navigation">
        {/* map() loops through navItems and renders one NavLink for each item.
            item is a local variable containing the current object from the array. */}
        {navItems.map((item) => (
          <NavLink
            // key is a special React value used to track list items between renders.
            key={item.label}
            // to is a NavLink prop that defines the route this link opens.
            to={item.to}
            state={item.to === '/profile' && location.pathname === '/results'
              ? { returnToResults: location.state }
              : undefined}
            // NavLink gives us isActive. If the current URL matches item.to,
            // the active class is added and CSS highlights the selected page.
            className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
          >
            <img src={item.icon} alt="" className="nav-icon" aria-hidden="true" />
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <p className="source-note">
        <span>Data sources</span>
        EU CosIng · EU Annexes
        <br />
        Condition rules reviewed for cosmetic guidance
        <br />
        <br />
        Not medical advice
      </p>
    </aside>
  )
}

export default Navbar
