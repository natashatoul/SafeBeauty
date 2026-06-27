import { Link } from "react-router-dom";

function Navbar() {
    return (
        <nav>
            <Link to="/">Home</Link>
            <Link to="/scan">Scan</Link>
            <Link to="/manual">Manual Input</Link>
            <Link to="/profile">Profile</Link>
        </nav>
    )
}
export default Navbar