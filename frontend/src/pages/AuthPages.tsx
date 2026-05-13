import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { ApiError } from '../types';

// ── Shared input validation (client-side; backend re-validates everything) ──

function validateUsername(v: string): string | null {
  if (v.length < 3)  return 'Username must be at least 3 characters.';
  if (v.length > 50) return 'Username must be at most 50 characters.';
  if (!/^[a-zA-Z0-9_]+$/.test(v))
    return 'Username may only contain letters, digits, and underscores.';
  return null;
}

function validatePassword(v: string): string | null {
  if (v.length < 8) return 'Password must be at least 8 characters.';
  return null;
}

// ── Login ──────────────────────────────────────────────────────────────────

export function LoginPage() {
  const { login } = useAuth();
  const navigate  = useNavigate();
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
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthForm
      title="Sign In"
      onSubmit={handleSubmit}
      error={error}
      loading={loading}
      footer={<>Don't have an account? <Link to="/register">Register</Link></>}
    >
      <Field label="Username" value={username} onChange={setUsername} />
      <Field label="Password" type="password" value={password} onChange={setPassword} />
    </AuthForm>
  );
}

// ── Register ───────────────────────────────────────────────────────────────

export function RegisterPage() {
  const { register } = useAuth();
  const navigate     = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirm,  setConfirm]  = useState('');
  const [error,    setError]    = useState('');
  const [loading,  setLoading]  = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');

    // Client-side validation – backend will also validate (defence in depth)
    const usernameErr = validateUsername(username);
    if (usernameErr) { setError(usernameErr); return; }

    const passwordErr = validatePassword(password);
    if (passwordErr) { setError(passwordErr); return; }

    if (password !== confirm) { setError('Passwords do not match.'); return; }

    setLoading(true);
    try {
      await register({ username, password });
      navigate('/');
    } catch (err) {
      setError((err as ApiError).message ?? 'Registration failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthForm
      title="Create Account"
      onSubmit={handleSubmit}
      error={error}
      loading={loading}
      footer={<>Already have an account? <Link to="/login">Sign in</Link></>}
    >
      <Field label="Username" value={username} onChange={setUsername}
             hint="3–50 chars, letters/digits/underscores only" />
      <Field label="Password" type="password" value={password} onChange={setPassword}
             hint="Minimum 8 characters" />
      <Field label="Confirm password" type="password" value={confirm} onChange={setConfirm} />
    </AuthForm>
  );
}

// ── Shared sub-components ──────────────────────────────────────────────────

interface AuthFormProps {
  title: string;
  onSubmit: (e: React.FormEvent) => void;
  error: string;
  loading: boolean;
  footer: React.ReactNode;
  children: React.ReactNode;
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
  label: string;
  type?: string;
  value: string;
  onChange: (v: string) => void;
  hint?: string;
}

function Field({ label, type = 'text', value, onChange, hint }: FieldProps) {
  const id = label.toLowerCase().replace(/\s+/g, '-');
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        type={type}
        value={value}
        autoComplete={type === 'password' ? 'current-password' : 'username'}
        onChange={e => onChange(e.target.value)}
        required
      />
      {hint && <small className="field-hint">{hint}</small>}
    </div>
  );
}
