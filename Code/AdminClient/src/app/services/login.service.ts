import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, filter, map, tap, of, catchError } from 'rxjs';
import { BaseService } from './base-service';
import { ConnectedUser } from '../interfaces/models';

@Injectable({
  providedIn: 'root',
})
export class LoginService extends BaseService {
  private readonly AUTH_TOKEN_KEY = 'auth_token';
  private readonly USER_DATA_KEY = 'user_data';

  private _isAuthenticated: boolean = false;
  private _pendingLoginEmail: string | null = null;

  login(email: string, password: string): Observable<any> {
    const url = 'AdminAdminUser/login';
    const loginData = { email, password }; // Create request body object matching LoginModel
    
    return this.post<any>(url, loginData).pipe(
      map((response) => {
        if (response.result) {
          if (response.data && response.data.requireOtp) {
            // OTP is required
            this._pendingLoginEmail = email;
            return { requireOtp: true };
          } else if (response.error === null) {
            // Successful login (null means no error)
            this._isAuthenticated = true;
            this.saveUserData(email);
            return { success: true };
          }
        }
        // Login failed with error message
        return { error: response.error || 'Authentication failed' };
      }),
      catchError((error) => {
        console.error('Login error:', error);
        return of({ error: 'An unexpected error occurred. Please try again later.' });
      })
    );
  }

  verifyLoginOtp(otp: string): Observable<any> {
    if (!this._pendingLoginEmail) {
      return of({ error: 'No pending login to verify' });
    }

    const url = 'AdminAdminUser/verifyLoginOtp';
    const data = { email: this._pendingLoginEmail, otp };
    
    return this.post<any>(url, data).pipe(
      map((response) => {
        if (response.result && response.error === null) {
          // OTP verification successful
          this._isAuthenticated = true;
          this.saveUserData(this._pendingLoginEmail!);
          this._pendingLoginEmail = null;
          return { success: true };
        } else {
          // Verification failed
          return { error: response.error || 'OTP verification failed' };
        }
      }),
      catchError((error) => {
        console.error('OTP verification error:', error);
        return of({ error: 'An unexpected error occurred. Please try again later.' });
      })
    );
  }

  logout(): void {
    // First, clear local storage
    localStorage.removeItem(this.AUTH_TOKEN_KEY);
    localStorage.removeItem(this.USER_DATA_KEY);
    this._isAuthenticated = false;
    
    // Make a call to the server to invalidate the authentication cookie
    const url = 'AdminAdminUser/logout';
    this.get(url).pipe(
      catchError(() => of(null))
    ).subscribe(() => {
      // Navigate to login page after logout attempt (successful or not)
      this.router.navigate(['/login']);
    });
  }

  checkAuthStatus(): Observable<boolean> {
    const url = 'AdminAdminUser/validateAuth';
    
    return this.get<boolean>(url).pipe(
      map((response) => {
        this._isAuthenticated = response.result && response.data === true;
        return this._isAuthenticated;
      }),
      catchError(() => {
        this._isAuthenticated = false;
        return of(false);
      })
    );
  }

  isAuthenticated(): boolean {
    return this._isAuthenticated;
  }

  getCurrentUser(): ConnectedUser {
    const userData = localStorage.getItem(this.USER_DATA_KEY);
    return userData ? JSON.parse(userData) : null;
  }
  
  forgotPassword(email: string): Observable<string | null> {
    const url = 'AdminAdminUser/forgotPassword';
    const requestData = { email };
    
    return this.post<any>(url, requestData).pipe(
      map((response) => {
        if (response.result && response.error === null) {
          return null;
        } else {
          return response.error || 'Failed to process request';
        }
      }),
      catchError((error) => {
        console.error('Forgot password error:', error);
        return of('An unexpected error occurred. Please try again later.');
      })
    );
  }
  
  resetPassword(email: string, otp: string, newPassword: string): Observable<string | null> {
    const url = 'AdminAdminUser/resetPassword';
    const requestData = { email, otp, newPassword };
    
    return this.post<any>(url, requestData).pipe(
      map((response) => {
        if (response.result && response.error === null) {
          return null;
        } else {
          return response.error || 'Failed to reset password';
        }
      }),
      catchError((error) => {
        console.error('Password reset error:', error);
        return of('An unexpected error occurred. Please try again later.');
      })
    );
  }
  
  changePassword(currentPassword: string, newPassword: string): Observable<string | null> {
    const url = 'AdminAdminUser/ChangePassword';
    const requestData = { currentPassword, newPassword };
    
    return this.post<any>(url, requestData).pipe(
      map((response) => {
        if (response.result && response.error === null) {
          return null;
        } else {
          return response.error || 'Failed to change password';
        }
      }),
      catchError((error) => {
        console.error('Change password error:', error);
        return of('An unexpected error occurred. Please try again later.');
      })
    );
  }

  private saveUserData(email: string): void {
    // Store minimal user data
    const userData = { email };
    localStorage.setItem(this.USER_DATA_KEY, JSON.stringify(userData));
  }
}
