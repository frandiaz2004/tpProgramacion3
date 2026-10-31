import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, computed, signal, type OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from './auth/user.service';
import {
  LucideActivity,
  LucideBookOpen,
  LucideCalendarDays,
  LucideDumbbell,
  LucideGraduationCap,
  LucidePencil,
  LucidePlus,
  LucideSearch,
  LucideTrash2,
  LucideTrophy,
  LucideUsers,
  LucideX,
} from '@lucide/angular';

type EntityKey = 'Players' | 'Teams' | 'Trainers' | 'Students' | 'Courses' | 'Activities';
type FieldType = 'text' | 'number' | 'date' | 'select';
type EntityRecord = Record<string, unknown>;

interface FieldDefinition {
  key: string;
  label: string;
  type?: FieldType;
  required?: boolean;
  min?: number;
  max?: number;
  minLength?: number;
  maxLength?: number;
  placeholder?: string;
  options?: { label: string; value: number }[];
}

interface SectionDefinition {
  key: EntityKey;
  label: string;
  singular: string;
  plural: string;
  description: string;
  fields: FieldDefinition[];
}

const personFields: FieldDefinition[] = [
  { key: 'name', label: 'Nombre', required: true, minLength: 3, maxLength: 100, placeholder: 'Nombre y apellido' },
  { key: 'age', label: 'Edad', type: 'number', required: true, min: 1, max: 100 },
  { key: 'dni', label: 'DNI', maxLength: 20, placeholder: 'Opcional' },
];

const sections: SectionDefinition[] = [
  { key: 'Players', label: 'Jugadores', singular: 'jugador', plural: 'Jugadores', description: 'Plantel y datos de cada jugador.', fields: [...personFields, { key: 'number', label: 'Dorsal', type: 'number', required: true, min: 1, max: 99 }] },
  { key: 'Teams', label: 'Equipos', singular: 'equipo', plural: 'Equipos', description: 'Equipos y categorías registradas.', fields: [{ key: 'name', label: 'Nombre', required: true, minLength: 3, maxLength: 100, placeholder: 'Nombre del equipo' }, { key: 'category', label: 'Categoría', required: true, minLength: 3, maxLength: 50, placeholder: 'Ej. Primera' }] },
  { key: 'Trainers', label: 'Entrenadores', singular: 'entrenador', plural: 'Entrenadores', description: 'Entrenadores y sus datos personales.', fields: personFields },
  { key: 'Students', label: 'Alumnos', singular: 'alumno', plural: 'Alumnos', description: 'Alumnos y legajos académicos.', fields: [...personFields, { key: 'file', label: 'Legajo', required: true, minLength: 1, maxLength: 20, placeholder: 'Número de legajo' }] },
  { key: 'Courses', label: 'Cursos', singular: 'curso', plural: 'Cursos', description: 'Cursos disponibles en el programa.', fields: [{ key: 'name', label: 'Nombre', required: true, minLength: 3, maxLength: 100, placeholder: 'Nombre del curso' }] },
  {
    key: 'Activities', label: 'Actividades', singular: 'actividad', plural: 'Actividades', description: 'Clases, exámenes y entregas de los cursos.', fields: [
      { key: 'title', label: 'Título', required: true, minLength: 3, maxLength: 150, placeholder: 'Título de la actividad' },
      { key: 'description', label: 'Descripción', required: true, minLength: 3, maxLength: 500, placeholder: 'Detalle de la actividad' },
      { key: 'date', label: 'Fecha', type: 'date' },
      { key: 'type', label: 'Tipo', type: 'select', required: true, options: [{ label: 'Clase', value: 0 }, { label: 'Examen', value: 1 }, { label: 'Entrega', value: 2 }] },
    ],
  },
];

@Component({
  selector: 'app-dashboard',
  imports: [
    FormsModule,
    LucideActivity,
    LucideBookOpen,
    LucideCalendarDays,
    LucideDumbbell,
    LucideGraduationCap,
    LucidePencil,
    LucidePlus,
    LucideSearch,
    LucideTrash2,
    LucideTrophy,
    LucideUsers,
    LucideX,
  ],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnDestroy {
  protected readonly selectedKey = signal<EntityKey>('Players');
  protected readonly records = signal<EntityRecord[]>([]);
  protected readonly query = signal('');
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly initialMembers = signal<EntityRecord[]>([]);
  protected readonly membersLoading = signal(false);
  protected readonly memberLoadError = signal<string | null>(null);
  protected selectedMemberIds: number[] = [];
  protected readonly memberDefinition = computed(() => {
    if (this.editingId() !== null) return null;
    if (this.selectedKey() === 'Teams') return {
      resource: 'Players', key: 'playerIds', label: 'Jugadores',
      empty: 'Primero registrá un jugador en la sección Jugadores.',
    };
    if (this.selectedKey() === 'Courses') return {
      resource: 'Students', key: 'studentIds', label: 'Alumnos',
      empty: 'Primero registrá un alumno en la sección Alumnos.',
    };
    return null;
  });
  private memberRequest?: Subscription;
  protected readonly section = computed(() => sections.find((item) => item.key === this.selectedKey())!);
  protected readonly filteredRecords = computed(() => {
    const query = this.query().trim().toLocaleLowerCase();
    if (!query) return this.records();
    return this.records().filter((record) => this.section().fields.some((field) => this.cellValue(record, field).toLocaleLowerCase().includes(query)));
  });
  protected draft: Record<string, string | number | null> = {};

  constructor(private readonly http: HttpClient, private readonly user: UserService, private readonly router: Router) {
    this.loadRecords();
  }

  ngOnDestroy(): void {
    this.memberRequest?.unsubscribe();
  }

  protected logout(): void {
    this.user.logout();
    void this.router.navigateByUrl('/login');
  }

  protected selectSection(key: EntityKey): void {
    this.selectedKey.set(key);
    this.query.set('');
    this.successMessage.set(null);
    this.closeForm();
    this.loadRecords();
  }

  protected loadRecords(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.http.get<EntityRecord[]>(`/${this.selectedKey()}`).subscribe({
      next: (records) => {
        this.records.set(records);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.records.set([]);
        this.errorMessage.set(this.describeError(error));
        this.loading.set(false);
      },
    });
  }

  protected openCreateForm(): void {
    this.memberRequest?.unsubscribe();
    this.editingId.set(null);
    this.initialMembers.set([]);
    this.selectedMemberIds = [];
    this.memberLoadError.set(null);
    this.membersLoading.set(false);
    this.errorMessage.set(null);
    this.draft = Object.fromEntries(this.section().fields.map((field) => [field.key, field.key === 'type' ? 0 : '']));
    this.formOpen.set(true);
    this.successMessage.set(null);
    this.loadInitialMembers();
  }

  protected loadInitialMembers(): void {
    const definition = this.memberDefinition();
    if (!definition) return;
    this.memberRequest?.unsubscribe();
    this.memberLoadError.set(null);
    this.membersLoading.set(true);
    this.memberRequest = this.http.get<EntityRecord[]>(`/${definition.resource}`).subscribe({
      next: (members) => {
        this.initialMembers.set(members);
        this.membersLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.memberLoadError.set(this.describeError(error));
        this.membersLoading.set(false);
      },
    });
  }

  protected openEditForm(record: EntityRecord): void {
    this.memberRequest?.unsubscribe();
    this.membersLoading.set(false);
    this.memberLoadError.set(null);
    this.errorMessage.set(null);
    this.editingId.set(Number(record['id']));
    this.draft = Object.fromEntries(this.section().fields.map((field) => {
      const value = record[field.key];
      return [field.key, field.key === 'date' && typeof value === 'string' ? value.slice(0, 10) : value == null ? '' : value as string | number];
    }));
    this.formOpen.set(true);
    this.successMessage.set(null);
  }

  protected saveRecord(valid: boolean): void {
    if (!valid) return;
    const membership = this.memberDefinition();
    if (membership && (this.membersLoading() || this.memberLoadError() || this.selectedMemberIds.length === 0)) return;
    const body: Record<string, unknown> = Object.fromEntries(this.section().fields.map((field) => {
      const value = this.draft[field.key];
      if (value === '' || value == null) return [field.key, null];
      return [field.key, field.type === 'number' || field.type === 'select' ? Number(value) : value];
    }));
    if (membership) body[membership.key] = [...this.selectedMemberIds];
    const id = this.editingId();
    const request = id === null
      ? this.http.post(`/${this.selectedKey()}`, body)
      : this.http.put(`/${this.selectedKey()}/${id}`, body);

    request.subscribe({
      next: () => {
        this.formOpen.set(false);
        this.successMessage.set(id === null ? 'Registro creado.' : 'Cambios guardados.');
        this.loadRecords();
      },
      error: (error: HttpErrorResponse) => this.errorMessage.set(this.describeError(error)),
    });
  }

  protected deleteRecord(record: EntityRecord): void {
    const id = Number(record['id']);
    const label = this.section().fields.map((field) => record[field.key]).find((value) => typeof value === 'string') ?? `#${id}`;
    if (!window.confirm(`¿Eliminar "${label}"? Esta acción no se puede deshacer.`)) return;

    this.http.delete(`/${this.selectedKey()}/${id}`).subscribe({
      next: () => {
        this.successMessage.set('Registro eliminado.');
        this.loadRecords();
      },
      error: (error: HttpErrorResponse) => this.errorMessage.set(this.describeError(error)),
    });
  }

  protected cellValue(record: EntityRecord, field: FieldDefinition): string {
    const value = record[field.key];
    if (value == null || value === '') return '—';
    if (field.key === 'type') return field.options?.find((option) => option.value === Number(value))?.label ?? String(value);
    if (field.type === 'date' && typeof value === 'string') return value.slice(0, 10);
    return String(value);
  }

  protected closeForm(): void {
    this.memberRequest?.unsubscribe();
    this.membersLoading.set(false);
    this.formOpen.set(false);
  }

  private describeError(error: HttpErrorResponse): string {
    if (error.status === 0) return 'No se pudo conectar con la API. Iniciá Tp3prog y verificá que esté disponible en localhost:5175.';
    if (error.status === 404) return 'La API no encontró el recurso solicitado.';
    if (error.status === 400) return 'La API rechazó los datos. Revisá los campos y sus validaciones.';
    if (error.status === 409) {
      const detail: unknown = typeof error.error === 'string' ? error.error : error.error?.detail;
      return typeof detail === 'string' ? detail : 'La operación entra en conflicto con los registros relacionados.';
    }
    if (error.status === 500) return 'El proxy no pudo acceder al backend. Iniciá Tp3prog en localhost:5175 y verificá su conexión a MySQL.';
    return `La API respondió con un error (${error.status}). Intentá nuevamente.`;
  }
}

