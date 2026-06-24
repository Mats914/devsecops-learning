import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider }  from './contexts/AuthContext';
import { Navbar }        from './components/Navbar';
import { LoginPage, RegisterPage } from './pages/AuthPages';
import { PostsPage }     from './pages/PostsPage';

// Rot-komponenten – sätter upp routing och auth runt hela appen
export default function App() {
  return (
    // AuthProvider måste wrappa allt så att useAuth() funkar överallt
    <AuthProvider>
      <BrowserRouter>
        <Navbar />
        <main className="main-content">
          <Routes>
            <Route path="/"         element={<PostsPage />} />
            <Route path="/login"    element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            {/* Okända URL:er skickas tillbaka till startsidan */}
            <Route path="*"         element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </BrowserRouter>
    </AuthProvider>
  );
}
