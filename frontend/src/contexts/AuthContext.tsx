import React, { createContext, useContext, useState, useCallback } from 'react';
import { authApi, tokenStore } from '../api/client';
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types';

interface AuthContextValue {
  user: { username: string; role: string } | null;
  login:    (data: LoginRequest)    => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout:   ()                      => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<{ username: string; role: string } | null>(null);

  const handleAuthResponse = useCallback((res: AuthResponse) => {
    tokenStore.set(res.token);
    setUser({ username: res.username, role: res.role });
  }, []);

  const login = useCallback(async (data: LoginRequest) => {
    const res = await authApi.login(data);
    handleAuthResponse(res);
  }, [handleAuthResponse]);

  const register = useCallback(async (data: RegisterRequest) => {
    const res = await authApi.register(data);
    handleAuthResponse(res);
  }, [handleAuthResponse]);

  const logout = useCallback(() => {
    tokenStore.clear();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, login, register, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
