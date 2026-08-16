import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly certificateFile = signal<File | null>(null);

  protected onCertificateChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.certificateFile.set(input.files?.[0] ?? null);
  }

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.pattern(/^[\p{L}\p{M}\p{S}\p{N}\p{P}]+$/u)]],
    email: ['', [Validators.required, Validators.email]],
    fullName: ['', [Validators.required]],
    role: ['user' as 'user' | 'sponsor', [Validators.required]],
    companyName: [''],
    ssmNumber: [''],
  });

  readonly submitting = signal(false);

  constructor() {
    this.form.controls.role.valueChanges.subscribe((role) => {
      const validators = role === 'sponsor' ? [Validators.required] : [];
      const ssmValidators = role === 'sponsor' ? [Validators.required, Validators.pattern(/^\d{7}-[A-Z]$|^\d{12}$/)] : [];
      this.form.controls.companyName.setValidators(validators);
      this.form.controls.ssmNumber.setValidators(ssmValidators);
      this.form.controls.companyName.updateValueAndValidity();
      this.form.controls.ssmNumber.updateValueAndValidity();
    });
  }

  get isSponsor() {
    return this.form.controls.role.value === 'sponsor';
  }

  async onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.isSponsor && !this.certificateFile()) {
      this.toast.error('Please attach your registration certificate.');
      return;
    }
    this.submitting.set(true);
    const { username, email, fullName, role, companyName, ssmNumber } = this.form.getRawValue();

    try {
      await this.auth.register({
        username,
        email,
        fullName,
        role,
        companyName: role === 'sponsor' ? companyName : undefined,
        ssmNumber: role === 'sponsor' ? ssmNumber : undefined,
      },
      role === 'sponsor' ? this.certificateFile()! : undefined
    );
      this.submitting.set(false);
      this.toast.success(
        role === 'sponsor'
          ? 'Registration submitted. Your account is pending admin approval.'
          : 'Registration successful. Check your email for the credentials to login.',
      );
      await this.router.navigateByUrl('/login');
    } catch (err) {
      this.submitting.set(false);
      if (err instanceof HttpErrorResponse && err.status === 409 && typeof err.error === 'string') {
        this.toast.error(err.error);
      } else {
        this.toast.error('Registration could not be completed.');
      }
    }
  }
}
