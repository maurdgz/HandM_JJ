import type { TokenDto, UsuarioDto } from "@/lib/types";

const TOKEN_KEY = "importnet.token";
const EXPIRATION_KEY = "importnet.expiration";
const USER_KEY = "importnet.user";
const ROLES_KEY = "importnet.roles";

export function clearSession(): void {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(EXPIRATION_KEY);
  sessionStorage.removeItem(USER_KEY);
  sessionStorage.removeItem(ROLES_KEY);
}

export function saveToken(token: TokenDto): void {
  sessionStorage.setItem(TOKEN_KEY, token.accessToken);
  sessionStorage.setItem(EXPIRATION_KEY, token.expiresAtUtc);
}

export function saveUser(user: UsuarioDto): void {
  sessionStorage.setItem(USER_KEY, JSON.stringify(user));
  sessionStorage.setItem(ROLES_KEY, JSON.stringify(user.roles));
}

export function getToken(): string | null {
  const token = sessionStorage.getItem(TOKEN_KEY);
  const expiration = sessionStorage.getItem(EXPIRATION_KEY);

  if (!token || !expiration) {
    clearSession();
    return null;
  }

  const expiresAt = Date.parse(expiration);

  if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
    clearSession();
    return null;
  }

  return token;
}

export function logout(): void {
  clearSession();
  window.location.replace("/login");
}