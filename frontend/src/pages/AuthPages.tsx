import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { ApiError } from '../types';

// ── Valideringsregler (samma logik som i backend/test) ──────────────────────

const rules = {
  username: (v: string) => {
    if (v.length < 3)  return 'Min 3 characters';
    if (v.length > 50) return 'Max 50 characters';
    if (!/^[a-zA-Z0-9_]+$/.test(v)) return 'Letters, digits and _ only';
    return null;
  },
  password: (v: string) => {
    if (v.length < 8)               return 'Min 8 characters';
    if (!/[A-Z]/.test(v))           return 'At least one uppercase letter';
    if (!/[a-z]/.test(v))           return 'At least one lowercase letter';
    if (!/[0-9]/.test(v))           return 'At least one digit';
    if (!/[\W_]/.test(v))           return 'At least one special character';
    return null;
  },
};

// ── Inloggning ──────────────────────────────────────────────────────────────

export function LoginPage() {
  const { login }   = useAuth();
  const navigate    = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error,    setError]    = useState('');
  const [loading,  setLoading]  = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login({ username, password });
      navigate('/');
    } catch (err) {
      setError((err as ApiError).message ?? 'Login failed.');
    } finally { setLoading(false); }
  }

  return (
    <AuthForm title="Sign In" onSubmit={handleSubmit} error={error} loading={loading}
      footer={<>No account? <Link to="/register">Register</Link></>}>
      <Field label="Username" value={username} onChange={setUsername} />
      <Field label="Password" type="password" value={password} onChange={setPassword} />
    </AuthForm>
  );
}

// ── Registrering ────────────────────────────────────────────────────────────

export function RegisterPage() {
  const { register } = useAuth();
  const navigate     = useNavigate();
  const [username, setUsername] = useState('');
  const [email,    setEmail]    = useState('');
  const [password, setPassword] = useState('');
  const [confirm,  setConfirm]  = useState('');
  const [error,    setError]    = useState('');
  const [loading,  setLoading]  = useState(false);
  const [strength, setStrength] = useState(0);

  // Enkel styrkemätare – räknar hur många krav som uppfylls
  function calcStrength(p: string) {
    let s = 0;
    if (p.length >= 8)      s++;
    if (/[A-Z]/.test(p))    s++;
    if (/[0-9]/.test(p))    s++;
    if (/[\W_]/.test(p))    s++;
    setStrength(s);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    const uErr = rules.username(username); if (uErr) { setError(uErr); return; }
    const pErr = rules.password(password); if (pErr) { setError(pErr); return; }
    if (password !== confirm) { setError('Passwords do not match.'); return; }

    setLoading(true);
    try {
      await register({ username, password, email: email || undefined });
      navigate('/');
    } catch (err) {
      setError((err as ApiError).message ?? 'Registration failed.');
    } finally { setLoading(false); }
  }

  const strengthLabel = ['', 'Weak', 'Fair', 'Good', 'Strong'][strength];
  const strengthColor = ['', '#e05c5c', '#f5a623', '#4f8ef7', '#27ae60'][strength];

  return (
    <AuthForm title="Create Account" onSubmit={handleSubmit} error={error} loading={loading}
      footer={<>Have an account? <Link to="/login">Sign in</Link></>}>
      <Field label="Username" value={username} onChange={setUsername}
             hint="3–50 chars, letters/digits/underscores" />
      <Field label="Email (optional)" type="email" value={email} onChange={setEmail}
             hint="Used for verification and password reset" />
      <Field label="Password" type="password" value={password} onChange={v => { setPassword(v); calcStrength(v); }}
             hint="Min 8 chars with uppercase, digit and special char">
        {password && (
          <div className="strength-bar">
            <div className="strength-fill" style={{ width: `${strength * 25}%`, background: strengthColor }} />
            <span style={{ color: strengthColor }}>{strengthLabel}</span>
          </div>
        )}
      </Field>
      <Field label="Confirm password" type="password" value={confirm} onChange={setConfirm} />
    </AuthForm>
  );
}

// ── Delade formulärkomponenter ──────────────────────────────────────────────

interface AuthFormProps {
  title: string; onSubmit: (e: React.FormEvent) => void;
  error: string; loading: boolean;
  footer: React.ReactNode; children: React.ReactNode;
}

function AuthForm({ title, onSubmit, error, loading, footer, children }: AuthFormProps) {
  return (
    <div className="auth-container">
      <div className="auth-card">
        <h1>{title}</h1>
        <form onSubmit={onSubmit} noValidate>
          {children}
          {error && <p className="error-msg" role="alert">{error}</p>}
          <button type="submit" disabled={loading} className="btn btn-primary">
            {loading ? 'Please wait…' : title}
          </button>
        </form>
        <p className="auth-footer">{footer}</p>
      </div>
    </div>
  );
}

interface FieldProps {
  label: string; type?: string; value: string;
  onChange: (v: string) => void; hint?: string; children?: React.ReactNode;
}
function Field({ label, type = 'text', value, onChange, hint, children }: FieldProps) {
  const id = label.toLowerCase().replace(/\s+/g, '-');
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input id={id} type={type} value={value} required
        autoComplete={type === 'password' ? 'current-password' : type === 'email' ? 'email' : 'username'}
        onChange={e => onChange(e.target.value)} />
      {children}
      {hint && <small className="field-hint">{hint}</small>}
    </div>
  );
}
