import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { UserService } from './user.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  readonly user = inject(UserService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  readonly busy = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  submit(): void {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.errorMessage.set(null);
    this.busy.set(true);
    this.user.login(this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.busy.set(false);
        this.form.controls.password.reset();
      }),
    ).subscribe({
      next: () => {
        void this.router.navigateByUrl('/dashboard', { replaceUrl: true });
      },
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : null;
        this.errorMessage.set(status === 401 ? 'Email o contraseña incorrectos.'
          : status === 0 ? 'No se pudo conectar con la API. Intentá nuevamente.'
          : status === 400 ? 'Revisá el email y la contraseña ingresados.'
          : 'No se pudo iniciar sesión. Intentá nuevamente más tarde.');
      },
    });
  }
}
