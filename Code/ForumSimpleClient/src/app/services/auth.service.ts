import { Injectable, signal } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BaseService } from './base.service';
import { AuthData, AuthResult } from '../models/forum.models';

@Injectable({ providedIn: 'root' })
export class AuthService extends BaseService {
  private static readonly TOKEN_KEY = 'forum_token';
  private static readonly USER_KEY = 'forum_user';

  private readonly _currentUser = signal<AuthResult | null>(this.readStoredUser());
  readonly currentUser = this._currentUser.asReadonly();

  login(name: string, password: string): Observable<AuthResult> {
    return this.post<AuthData>('forum/login', { name, password }).pipe(
      map((response) => {
        if (!response.result || !response.data?.auth) {
          throw new Error(response.error ?? 'Login failed.');
        }

        this.storeAuth(response.data.auth);
        return response.data.auth;
      })
    );
  }

  register(
    name: string,
    password: string,
    isManager: boolean,
    profilePicture?: File | null
  ): Observable<AuthResult> {
    const formData = new FormData();
    formData.append('name', name);
    formData.append('password', password);
    formData.append('isManager', String(isManager));

    if (profilePicture) {
      formData.append('profilePicture', profilePicture);
    }

    return this.post<AuthData>('forum/register', formData).pipe(
      map((response) => {
        if (!response.result || !response.data?.auth) {
          throw new Error(response.error ?? 'Registration failed.');
        }

        this.storeAuth(response.data.auth);
        return response.data.auth;
      })
    );
  }

  logout(): void {
    localStorage.removeItem(AuthService.TOKEN_KEY);
    localStorage.removeItem(AuthService.USER_KEY);
    this._currentUser.set(null);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getToken(): string | null {
    return localStorage.getItem(AuthService.TOKEN_KEY);
  }

  private storeAuth(auth: AuthResult): void {
    localStorage.setItem(AuthService.TOKEN_KEY, auth.token);
    localStorage.setItem(AuthService.USER_KEY, JSON.stringify(auth));
    this._currentUser.set(auth);
  }

  private readStoredUser(): AuthResult | null {
    const rawUser = localStorage.getItem(AuthService.USER_KEY);
    if (!rawUser) {
      return null;
    }

    try {
      return JSON.parse(rawUser) as AuthResult;
    } catch {
      localStorage.removeItem(AuthService.USER_KEY);
      return null;
    }
  }
}
