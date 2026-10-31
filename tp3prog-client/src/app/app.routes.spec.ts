import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes, sessionGuard } from './app.routes';
import { UserService } from './auth/user.service';

describe('sessionGuard', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)] }));
  it('redirige a login sin sesión', () => {
    const result = TestBed.runInInjectionContext(() => sessionGuard({} as never, {} as never));
    expect(result instanceof UrlTree).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });

  it('conserva el panel y al cerrar sesión bloquea volver por URL', async () => {
    const service = TestBed.inject(UserService);
    const http = TestBed.inject(HttpTestingController);
    service.login({ email: 'ana@example.com', password: 'password' }).subscribe();
    http.expectOne('/User/login').flush({ token: 'jwt-demo', expiresAt: '2099-01-01T00:00:00Z' });
    http.expectOne('/User/me').flush({ id: 1, email: 'ana@example.com' });
    const harness = await RouterTestingHarness.create('/dashboard');
    http.expectOne('/Players').flush([]);
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Panel de gestión');
    const logout = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find(button => button.textContent?.includes('Cerrar sesión'))!;
    logout.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(service.session()).toBeNull();
    await harness.navigateByUrl('/dashboard');
    expect(TestBed.inject(Router).url).toBe('/login');
    http.verify();
  });
});
