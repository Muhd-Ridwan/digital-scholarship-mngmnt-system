import { Component, inject } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { TopNavbar } from '../top-navbar/top-navbar';
import { ToastContainer } from '../toast-container/toast-container';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-dashboard-shell',
  standalone: true,
  imports: [RouterOutlet, TopNavbar, ToastContainer],
  templateUrl: './dashboard-shell.html',
})
export class DashboardShell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly profile = this.auth.profile;

  constructor() {
    this.loadProfile();
  }

  private async loadProfile() {
    const profile = await this.auth.getProfile();
    if (!profile) {
      await this.onLogout();
    }
  }

  public async onLogout() {
    await this.auth.logout();
    await this.router.navigateByUrl('/login');
  }
}
