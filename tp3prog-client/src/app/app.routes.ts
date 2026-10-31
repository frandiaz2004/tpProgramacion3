import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { UserService } from './auth/user.service';
import { Login } from './auth/login';
import { Dashboard } from './dashboard';

export const sessionGuard: CanActivateFn = () => inject(UserService).isAuthenticated()
  || inject(Router).createUrlTree(['/login']);

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'dashboard', component: Dashboard, canActivate: [sessionGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' },
];
