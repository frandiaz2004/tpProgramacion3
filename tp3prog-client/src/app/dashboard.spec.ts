import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Dashboard } from './dashboard';

describe('Dashboard: integrantes obligatorios', () => {
  let fixture: ComponentFixture<Dashboard>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dashboard], providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(Dashboard);
    http = TestBed.inject(HttpTestingController);
    http.expectOne('/Players').flush([]);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    http.verify();
    vi.restoreAllMocks();
  });

  it.each([
    { resource: 'Teams' as const, members: 'Players', field: 'playerIds' },
    { resource: 'Courses' as const, members: 'Students', field: 'studentIds' },
  ])('exige integrantes al crear $resource y envía sus IDs', ({ resource, members, field }) => {
    const dashboard = fixture.componentInstance;
    dashboard['selectSection'](resource);
    http.expectOne('/' + resource).flush([]);
    dashboard['openCreateForm']();
    dashboard['draft'] = { name: 'Nuevo registro', category: 'Primera' };
    dashboard['saveRecord'](true);
    http.expectNone(request => request.method === 'POST');
    http.expectOne('/' + members).flush([{ id: 7, name: 'Ana' }]);
    dashboard['saveRecord'](true);
    http.expectNone(request => request.method === 'POST');
    dashboard['selectedMemberIds'] = [7];
    dashboard['saveRecord'](true);
    const post = http.expectOne('/' + resource);
    expect(post.request.method).toBe('POST');
    expect(post.request.body[field]).toEqual([7]);
    post.flush({ id: 1 });
    http.expectOne('/' + resource).flush([]);
  });

  it.each(['Teams', 'Courses'] as const)('editar %s no carga ni reemplaza integrantes', resource => {
    const dashboard = fixture.componentInstance;
    dashboard['selectSection'](resource);
    http.expectOne('/' + resource).flush([]);
    dashboard['openEditForm']({ id: 5, name: 'Registro', category: 'Primera' });
    dashboard['saveRecord'](true);
    http.expectNone('/Players');
    http.expectNone('/Students');
    const put = http.expectOne('/' + resource + '/5');
    expect(put.request.method).toBe('PUT');
    expect(put.request.body['playerIds']).toBeUndefined();
    expect(put.request.body['studentIds']).toBeUndefined();
    put.flush(null);
    http.expectOne('/' + resource).flush([]);
  });

  it('permite reintentar la carga de alumnos y bloquea el alta si falla', () => {
    const dashboard = fixture.componentInstance;
    dashboard['selectSection']('Courses');
    http.expectOne('/Courses').flush([]);
    dashboard['openCreateForm']();
    http.expectOne('/Students').error(new ProgressEvent('error'));
    dashboard['saveRecord'](true);
    http.expectNone(request => request.method === 'POST');
    expect(dashboard['memberLoadError']()).toContain('conectar');
    dashboard['loadInitialMembers']();
    http.expectOne('/Students').flush([{ id: 3, name: 'Alumno' }]);
    expect(dashboard['memberLoadError']()).toBeNull();
  });

  it('muestra el motivo del conflicto sin quitar el registro del listado', () => {
    const dashboard = fixture.componentInstance;
    dashboard['loadRecords']();
    const player = { id: 1, name: 'Jugador' };
    http.expectOne('/Players').flush([player]);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    dashboard['deleteRecord'](player);
    http.expectOne('/Players/1').flush('No se puede eliminar al último jugador.',
      { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('último jugador');
    expect(dashboard['records']()).toEqual([player]);
  });

  it('cancela la carga de integrantes cuando se cierra el formulario', () => {
    const dashboard = fixture.componentInstance;
    dashboard['selectSection']('Courses');
    http.expectOne('/Courses').flush([]);
    dashboard['openCreateForm']();
    const request = http.expectOne('/Students');
    dashboard['closeForm']();
    expect(request.cancelled).toBe(true);
  });
});
