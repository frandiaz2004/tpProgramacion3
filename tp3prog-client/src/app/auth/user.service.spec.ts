import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(UserService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('envía las credenciales y valida el token sólo contra la API propia', () => {
    service.login({ email: 'ana@example.com', password: 'clave exacta ' }).subscribe();
    const request = http.expectOne('/User/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'ana@example.com', password: 'clave exacta ' });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ token: 'jwt-demo', expiresAt: '2099-01-01T00:00:00Z' });
    const profile = http.expectOne('/User/me');
    expect(profile.request.headers.get('Authorization')).toBe('Bearer jwt-demo');
    profile.flush({ id: 1, email: 'ana@example.com' });
    expect(service.session()?.token).toBe('jwt-demo');
    service.logout();
    expect(service.session()).toBeNull();
    expect(new UserService(TestBed.inject(HttpClient)).session()).toBeNull();
  });

  it('limpia la sesión cuando el servidor rechaza las credenciales', () => {
    let status = 0;
    service.login({ email: 'ana@example.com', password: 'incorrecta' }).subscribe({ error: error => status = error.status });
    http.expectOne('/User/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    expect(service.session()).toBeNull();
    http.expectNone('/User/me');
  });

  it('no conserva un token cuya validación falla', () => {
    service.login({ email: 'ana@example.com', password: 'password' }).subscribe({ error: () => {} });
    http.expectOne('/User/login').flush({ token: 'jwt-demo', expiresAt: '2099-01-01T00:00:00Z' });
    http.expectOne('/User/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(service.session()).toBeNull();
  });
  it('descarta la sesión al vencer según expiresAt', () => {
    service.login({ email: 'ana@example.com', password: 'password' }).subscribe();
    http.expectOne('/User/login').flush({ token: 'jwt-demo', expiresAt: '2099-01-01T00:00:00Z' });
    http.expectOne('/User/me').flush({ id: 1, email: 'ana@example.com' });
    const now = vi.spyOn(Date, 'now').mockReturnValue(Date.parse('2099-01-02T00:00:00Z'));
    try {
      expect(service.isAuthenticated()).toBe(false);
      expect(service.session()).toBeNull();
    } finally { now.mockRestore(); }
  });
});

