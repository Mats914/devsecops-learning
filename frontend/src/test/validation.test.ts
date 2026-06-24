import { describe, it, expect, beforeEach } from 'vitest';
import { tokenStore } from '../api/client';

// ── tokenStore – minneslagring av JWT ────────────────────────────────────────

describe('tokenStore', () => {
  beforeEach(() => tokenStore.clearTokens());

  it('starts empty', () => {
    expect(tokenStore.getAccess()).toBeNull();
    expect(tokenStore.getRefresh()).toBeNull();
  });

  it('stores both tokens', () => {
    tokenStore.setTokens('access-jwt', 'refresh-token');
    expect(tokenStore.getAccess()).toBe('access-jwt');
    expect(tokenStore.getRefresh()).toBe('refresh-token');
  });

  it('clears both tokens', () => {
    tokenStore.setTokens('a', 'r');
    tokenStore.clearTokens();
    expect(tokenStore.getAccess()).toBeNull();
    expect(tokenStore.getRefresh()).toBeNull();
  });
});

// ── Validering av användarnamn ───────────────────────────────────────────────

// Kopierad logik från AuthPages – testas här isolerat
function validateUsername(v: string): string | null {
  if (v.length < 3)  return 'Min 3 characters';
  if (v.length > 50) return 'Max 50 characters';
  if (!/^[a-zA-Z0-9_]+$/.test(v)) return 'Letters, digits and _ only';
  return null;
}

describe('validateUsername', () => {
  it('accepts valid username',           () => expect(validateUsername('alice_99')).toBeNull());
  it('rejects too-short username',       () => expect(validateUsername('ab')).not.toBeNull());
  it('rejects special chars',            () => expect(validateUsername('alice!')).not.toBeNull());
  it('rejects empty string',             () => expect(validateUsername('')).not.toBeNull());
  it('accepts underscores and digits',   () => expect(validateUsername('user_123')).toBeNull());
});

// ── Validering av lösenord ───────────────────────────────────────────────────

function validatePassword(v: string): string | null {
  if (v.length < 8)       return 'Min 8 characters';
  if (!/[A-Z]/.test(v))   return 'Need uppercase';
  if (!/[a-z]/.test(v))   return 'Need lowercase';
  if (!/[0-9]/.test(v))   return 'Need digit';
  if (!/[\W_]/.test(v))   return 'Need special char';
  return null;
}

describe('validatePassword', () => {
  it('accepts strong password',          () => expect(validatePassword('Secure1!')).toBeNull());
  it('rejects too short',                () => expect(validatePassword('Ab1!')).not.toBeNull());
  it('rejects missing uppercase',        () => expect(validatePassword('secure1!')).not.toBeNull());
  it('rejects missing digit',            () => expect(validatePassword('SecurePass!')).not.toBeNull());
  it('rejects missing special char',     () => expect(validatePassword('Secure123')).not.toBeNull());
});

// ── Lösenordsstyrka (samma som calcStrength i RegisterPage) ────────────────

function calcStrength(p: string): number {
  let s = 0;
  if (p.length >= 8)   s++;
  if (/[A-Z]/.test(p)) s++;
  if (/[0-9]/.test(p)) s++;
  if (/[\W_]/.test(p)) s++;
  return s;
}

describe('calcStrength', () => {
  it('returns 0 for empty',             () => expect(calcStrength('')).toBe(0));
  it('returns 4 for strong password',   () => expect(calcStrength('Secure1!')).toBe(4));
  it('returns 1 for only long',         () => expect(calcStrength('abcdefgh')).toBe(1));
  it('returns 2 for long + uppercase',  () => expect(calcStrength('Abcdefgh')).toBe(2));
});
