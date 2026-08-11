import { Component, computed, effect, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { ROLE_BADGE_CLASSES, UserRole } from '../../../shared/models/user.model';

// Admin-only, full-name-only: email is the DB unique index + Cognito identity (needs a
// Cognito attribute update, not a DB write); password stays with Cognito's own flows.
@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [LucideAngularModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.html',
})
export class AdminProfile {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly toastService = inject(ToastService);

  readonly profile = this.auth.profile;
  protected readonly saving = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required]],
  });

  protected readonly roleBadgeClass = computed(() => {
    const role = this.profile()?.role;
    return role ? ROLE_BADGE_CLASSES[role as UserRole] : '';
  });

  protected readonly memberSince = computed(() => {
    const createdAt = this.profile()?.createdAt;
    if (!createdAt) {
      return '—';
    }
    return new Date(createdAt).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  });

  constructor() {
    // Seed the form once the profile signal resolves; never clobber an in-progress edit.
    effect(() => {
      const current = this.profile();
      if (current && this.form.pristine) {
        this.form.patchValue({ fullName: current.fullName });
      }
    });
  }

  protected async onSave(): Promise<void> {
    if (this.form.invalid || this.form.pristine) {
      return;
    }
    this.saving.set(true);
    try {
      await this.auth.updateFullName(this.form.getRawValue().fullName.trim());
      this.form.markAsPristine();
      this.toastService.success('Profile updated.');
    } catch {
      this.toastService.error('Could not update profile.');
    } finally {
      this.saving.set(false);
    }
  }
}
