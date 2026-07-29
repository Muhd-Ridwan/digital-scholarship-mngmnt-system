import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { ROLE_ROUTES } from '../../role-routes';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule],
  templateUrl: './login.html',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  readonly newPasswordForm = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  readonly submitting = signal(false);
  // Using 2 different name here is to make SOC, where if during login already click toggle password on
  // Then if the user are new, then the toggle password will still follow the previous one (True) because only have 1 signal
  readonly showPassword = signal(false);
  readonly showNewPassword = signal(false);
  needsNewPassword = false;

  async onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const { username, password } = this.form.getRawValue();

    try {
      const result = await this.auth.login(username, password);

      if (result.status === 'newPasswordRequired') {
        this.needsNewPassword = true;
        return;
      }
      if (result.status === 'failed') {
        this.toast.error('Sign in could not be completed');
        return;
      }
      await this.routeToDashboard();
    } catch (err) {
      console.error('Sign-in error:', err);
      this.toast.error('Incorrect username or password.');
    } finally {
      this.submitting.set(false);
    }
  }

  async onSetNewPassword() {
    if (this.newPasswordForm.invalid) {
      this.newPasswordForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { newPassword } = this.newPasswordForm.getRawValue();

    try {
      const isSignedIn = await this.auth.completeNewPassword(newPassword);
      if (!isSignedIn) {
        this.toast.error('Could not set new password.');
        return;
      }
      await this.routeToDashboard();
    } catch {
      this.toast.error('Could not set new password.');
    } finally {
      this.submitting.set(false);
    }
  }

  togglePasswordVisibility() {
    this.showPassword.update((visible) => !visible);
  }

  toggleNewPasswordVisibility() {
    this.showNewPassword.update((visible) => !visible);
  }

  private async routeToDashboard() {
    const profile = await this.auth.getProfile();
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    if (profile && returnUrl) {
      await this.router.navigateByUrl(returnUrl);
      return;
    }
    const route = profile ? ROLE_ROUTES[profile.role] : undefined;

    if (!route) {
      await this.auth.logout();
      this.toast.error('Unable to determine your account role. Please contact support.');
      return;
    }
    await this.router.navigateByUrl(route);
  }
}
