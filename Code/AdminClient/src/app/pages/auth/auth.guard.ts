import {Injectable} from '@angular/core';
import {CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot} from '@angular/router';
import {Observable, Subscriber, of, switchMap} from 'rxjs';
import {Router} from '@angular/router';
import {AppService} from "../../services/app.service";
import {LoginService} from "../../services/login.service";

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(
    private router: Router,
    private loginService: LoginService
  ) {}

  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {

    // If already authenticated, return true immediately
    if (this.loginService.isAuthenticated()) {
      return true;
    }
    
    // Otherwise check authentication status from the server
    return this.loginService.checkAuthStatus().pipe(
      switchMap(isAuthenticated => {
        if (!isAuthenticated) {
          this.router.navigate(['/login']);
        }
        return of(isAuthenticated);
      })
    );
  }
} 