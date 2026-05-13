import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export function Navbar() {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <nav className="navbar">
      <Link to="/" className="nav-brand">
        🔐 DevSecOps Blog
      </Link>
      <div className="nav-links">
        {isAuthenticated ? (
          <>
            <span className="nav-user">
              {user?.username}
              {user?.role === 'Admin' && <span className="badge-admin">Admin</span>}
            </span>
            <button className="btn btn-secondary btn-sm" onClick={handleLogout}>
              Sign out
            </button>
          </>
        ) : (
          <>
            <Link to="/login"    className="btn btn-secondary btn-sm">Sign in</Link>
            <Link to="/register" className="btn btn-primary   btn-sm">Register</Link>
          </>
        )}
      </div>
    </nav>
  );
}
