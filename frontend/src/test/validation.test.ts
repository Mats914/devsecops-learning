import { describe, it, expect, beforeEach } from 'vitest';
import { tokenStore } from '../api/client';

// ── tokenStore tests ───────────────────────────────────────────────────────

describe('tokenStore', () => {
  beforeEach(() => tokenStore.clear());

  it('starts empty', () => {
    expect(tokenStore.get()).toBeNull();
  });

  it('stores and retrieves a token', () => {
    tokenStore.set('my.jwt.token');
    expect(tokenStore.get()).toBe('my.jwt.token');
  });

  it('clears the token', () => {
    tokenStore.set('my.jwt.token');
    tokenStore.clear();
    expect(tokenStore.get()).toBeNull();
  });
});

// ── Input validation tests (mirrors backend rules) ────────────────────────

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

describe('validateUsername', () => {
  it('accepts a valid username', () => {
    expect(validateUsername('alice_99')).toBeNull();
  });

  it('rejects too-short username', () => {
    expect(validateUsername('ab')).not.toBeNull();
  });

  it('rejects username with special chars', () => {
    expect(validateUsername('alice!')).not.toBeNull();
  });

  it('rejects empty string', () => {
    expect(validateUsername('')).not.toBeNull();
  });
});

describe('validatePassword', () => {
  it('accepts a password of 8+ chars', () => {
    expect(validatePassword('securePass1')).toBeNull();
  });

  it('rejects a password shorter than 8 chars', () => {
    expect(validatePassword('short')).not.toBeNull();
  });
});
