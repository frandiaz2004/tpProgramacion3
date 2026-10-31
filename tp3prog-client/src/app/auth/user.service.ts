import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, map, Observable, switchMap, tap, throwError } from 'rxjs';
import { LoginModel, LoginResponse, UserProfile } from './login.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly currentSession = signal<LoginResponse | null>(null);
  readonly session = this.currentSession.asReadonly();

  constructor(private readonly http: HttpClient) {}

  login(credentials: LoginModel): Observable<LoginResponse> {
    this.logout();
    return this.http.post<LoginResponse>('/User/login', credentials).pipe(
      switchMap(response => {
        if (!response.token || !Number.isFinite(Date.parse(response.expiresAt))) {
          return throwError(() => new Error('Respuesta de login inválida.'));
        }
        // El bearer se envía sólo a este endpoint, nunca a URLs externas.
        return this.http.get<UserProfile>('/User/me', {
          headers: new HttpHeaders({ Authorization: `Bearer ${response.token}` }),
        }).pipe(
          map(() => response),
          catchError(error => throwError(() => error instanceof HttpErrorResponse && error.status === 401
            ? new Error('No se pudo validar la sesión.') : error)),
        );
      }),
      tap(response => this.currentSession.set(response)),
      catchError((error: HttpErrorResponse | Error) => {
        this.logout();
        return throwError(() => error);
      }),
    );
  }

  isAuthenticated(): boolean {
    const session = this.currentSession();
    if (!session || Date.parse(session.expiresAt) <= Date.now()) {
      this.logout();
      return false;
    }
    return true;
  }

  logout(): void {
    this.currentSession.set(null);
  }
}
