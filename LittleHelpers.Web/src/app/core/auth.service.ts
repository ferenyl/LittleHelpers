import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface LoginResponse {
  token: string;
  username: string;
  userLevel: string;
}

export interface RenewTokenResponse {
  token: string;
}

export interface MenuItemDto {
  label: string;
  route: string;
}

export interface AuthSessionContext {
  token: string;
  username: string | null;
  userLevel: string | null;
}

export interface AuthSessionObserver {
  afterLogin?(context: AuthSessionContext): void | Promise<void>;
  beforeLogout?(context: AuthSessionContext): void | Promise<void>;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'lh_token';
  private readonly userIdClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
  private readonly sessionObservers = new Set<AuthSessionObserver>();

  private _token = signal<string | null>(localStorage.getItem(this.tokenKey));
  private _username = signal<string | null>(localStorage.getItem('lh_username'));
  private _userLevel = signal<string | null>(localStorage.getItem('lh_userlevel'));

  readonly isLoggedIn = computed(() => !!this._token());
  readonly userLevel = computed(() => this._userLevel());
  readonly username = computed(() => this._username());
  readonly token = computed(() => this._token());

  constructor(private http: HttpClient, private router: Router) {}

  login(username: string, password: string) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { username, password }).pipe(
      tap(res => {
        this.setSession(res.token, res.username, res.userLevel);
        const session = this.getSessionContext();
        if (session) {
          void this.notifyObservers('afterLogin', session);
        }
      })
    );
  }

  renewToken() {
    return this.http.post<RenewTokenResponse>(`${environment.apiUrl}/auth/renew`, {}).pipe(
      tap(res => {
        this.setSession(res.token, this._username(), this._userLevel());
      })
    );
  }

  async logout() {
    const session = this.getSessionContext();
    if (session) {
      await this.notifyObservers('beforeLogout', session);
    }

    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem('lh_username');
    localStorage.removeItem('lh_userlevel');
    this._token.set(null);
    this._username.set(null);
    this._userLevel.set(null);
    this.router.navigate(['/login']);
  }

  getMenu() {
    return this.http.get<MenuItemDto[]>(`${environment.apiUrl}/menu`);
  }

  registerSessionObserver(observer: AuthSessionObserver) {
    this.sessionObservers.add(observer);
    return () => this.sessionObservers.delete(observer);
  }

  private setSession(token: string, username: string | null, userLevel: string | null) {
    localStorage.setItem(this.tokenKey, token);
    this._token.set(token);

    if (username) {
      localStorage.setItem('lh_username', username);
      this._username.set(username);
    }

    if (userLevel) {
      localStorage.setItem('lh_userlevel', userLevel);
      this._userLevel.set(userLevel);
    }
  }

  getUserIdFromToken(): string | null {
    const token = this._token();
    if (!token) return null;

    const payloadPart = token.split('.')[1];
    if (!payloadPart) return null;

    const normalized = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');

    try {
      const payload = JSON.parse(atob(padded)) as Record<string, unknown>;
      const userId = payload[this.userIdClaim];
      return typeof userId === 'string' && userId.length > 0 ? userId : null;
    } catch {
      return null;
    }
  }

  private getSessionContext(): AuthSessionContext | null {
    const token = this._token();
    if (!token) {
      return null;
    }

    return {
      token,
      username: this._username(),
      userLevel: this._userLevel(),
    };
  }

  private async notifyObservers(
    hook: keyof AuthSessionObserver,
    context: AuthSessionContext
  ) {
    const pending = [...this.sessionObservers]
      .map(observer => observer[hook]?.(context))
      .filter((result): result is void | Promise<void> => result !== undefined);

    const results = await Promise.allSettled(pending);
    for (const result of results) {
      if (result.status === 'rejected') {
        console.error('Auth session observer failed.', result.reason);
      }
    }
  }
}
