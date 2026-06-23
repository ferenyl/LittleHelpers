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

export interface MenuItemDto {
  label: string;
  route: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'lh_token';
  private readonly userIdClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

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
        localStorage.setItem(this.tokenKey, res.token);
        localStorage.setItem('lh_username', res.username);
        localStorage.setItem('lh_userlevel', res.userLevel);
        this._token.set(res.token);
        this._username.set(res.username);
        this._userLevel.set(res.userLevel);
      })
    );
  }

  logout() {
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
}
