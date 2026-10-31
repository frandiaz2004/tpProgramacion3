import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let http: HttpTestingController;
  let router: Router;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Login], providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] }).compileComponents();
    fixture = TestBed.createComponent(Login);
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
  });
  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });
  function credentials(email = 'ana@example.com', password = 'password') {
    fixture.componentInstance.form.setValue({ email, password });
    fixture.detectChanges();
  }
  it('exige email válido y contraseña antes de enviar', () => {
    expect(fixture.componentInstance.form.invalid).toBe(true);
    credentials('incorrecto', '');
    fixture.componentInstance.submit();
    http.expectNone('/User/login');
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
  });
  it('evita envíos duplicados y entra al panel sólo después de validar el token', () => {
    credentials();
    fixture.componentInstance.submit();
    fixture.componentInstance.submit();
    const request = http.expectOne('/User/login');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
    request.flush({ token: 'token-visible', expiresAt: '2099-01-01T00:00:00Z' });
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(fixture.componentInstance.user.session()).toBeNull();
    http.expectOne('/User/me').flush({ id: 1, email: 'ana@example.com' });
    fixture.detectChanges();
    expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith('/dashboard', { replaceUrl: true });
    expect(fixture.componentInstance.user.session()?.token).toBe('token-visible');
    expect(fixture.nativeElement.textContent).not.toContain('token-visible');
    expect(fixture.componentInstance.form.controls.password.value).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain('Abrir panel');
  });
  it('muestra credenciales incorrectas y borra la contraseña ante 401', () => {
    credentials(); fixture.componentInstance.submit();
    http.expectOne('/User/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('Email o contraseña incorrectos');
    expect(fixture.componentInstance.form.controls.password.value).toBe('');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
  it('distingue una caída de conexión de credenciales incorrectas', () => {
    credentials(); fixture.componentInstance.submit();
    http.expectOne('/User/login').error(new ProgressEvent('error'));
    fixture.detectChanges();
    const message = fixture.nativeElement.querySelector('[role=alert]').textContent;
    expect(message).toContain('conectar');
    expect(message).not.toContain('contraseña incorrectos');
  });
  it('distingue un error del servidor de credenciales incorrectas', () => {
    credentials(); fixture.componentInstance.submit();
    http.expectOne('/User/login').flush({}, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('más tarde');
    expect(fixture.componentInstance.busy()).toBe(false);
  });
  it('permanece en el login si el endpoint protegido rechaza el token', () => {
    credentials(); fixture.componentInstance.submit();
    http.expectOne('/User/login').flush({ token: 'token-rechazado', expiresAt: '2099-01-01T00:00:00Z' });
    http.expectOne('/User/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('token-rechazado');
    expect(fixture.componentInstance.user.session()).toBeNull();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(fixture.componentInstance.busy()).toBe(false);
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).not.toContain('contraseña incorrectos');
  });
  it('permanece en el login si falla la conexión al validar la sesión', () => {
    credentials(); fixture.componentInstance.submit();
    http.expectOne('/User/login').flush({ token: 'jwt-demo', expiresAt: '2099-01-01T00:00:00Z' });
    http.expectOne('/User/me').error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(fixture.componentInstance.user.session()).toBeNull();
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('conectar');
  });
});

