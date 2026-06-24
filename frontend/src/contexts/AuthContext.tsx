import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { authApi, tokenStore } from '../api/client';
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types';

// Användarinfo vi sparar i context (inte hela JWT – bara det UI behöver)
interface User { username: string; role: string; emailVerified: boolean; }

interface AuthContextValue {
  user:            User | null;
  login:           (data: LoginRequest)    => Promise<void>;
  register:        (data: RegisterRequest) => Promise<void>;
  logout:          ()                      => Promise<void>;
  isAuthenticated: boolean;
  isAdmin:         boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  // Gemensam logik efter lyckad login/register/refresh
  const applyAuth = useCallback((res: AuthResponse) => {
    tokenStore.setTokens(res.accessToken, res.refreshToken);
    setUser({ username: res.username, role: res.role, emailVerified: res.emailVerified });
  }, []);

  const login = useCallback(async (data: LoginRequest) => {
    applyAuth(await authApi.login(data));
  }, [applyAuth]);

  const register = useCallback(async (data: RegisterRequest) => {
    applyAuth(await authApi.register(data));
  }, [applyAuth]);

  const logout = useCallback(async () => {
    const rt = tokenStore.getRefresh();
    // Försök ogiltigförklara refresh token på servern (ignorera fel om nätverket strular)
    if (rt) await authApi.revoke(rt).catch(() => {});
    tokenStore.clearTokens();
    setUser(null);
  }, []);

  // Håll sessionen vid liv – refresha innan access token går ut
  useEffect(() => {
    if (!user) return;
    const interval = setInterval(async () => {
      const rt = tokenStore.getRefresh();
      if (!rt) return;
      try {
        const res = await authApi.refresh(rt);
        applyAuth(res);
      } catch {
        logout();
      }
    }, 12 * 60 * 1000); // var 12:e minut (access token håller i 15 min)
    return () => clearInterval(interval);
  }, [user, applyAuth, logout]);

  return (
    <AuthContext.Provider value={{
      user,
      login,
      register,
      logout,
      isAuthenticated: !!user,
      isAdmin: user?.role === 'Admin',
    }}>
      {children}
    </AuthContext.Provider>
  );
}

// Hook för att hämta auth-state – kraschar om man glömmer AuthProvider
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be inside AuthProvider');
  return ctx;
}
