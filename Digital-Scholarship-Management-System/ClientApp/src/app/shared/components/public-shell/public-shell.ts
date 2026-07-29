import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PublicTopbar } from '../public-topbar/public-topbar';
import { TopNavbar } from '../top-navbar/top-navbar';
import { ToastContainer } from '../toast-container/toast-container';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-public-shell',
  standalone: true,
  imports: [RouterOutlet, PublicTopbar, ToastContainer, TopNavbar],
  templateUrl: './public-shell.html',
})
export class PublicShell {
  private readonly auth = inject(AuthService);

  readonly profile = this.auth.profile;

  constructor() {
    this.auth.getProfile();
  }

  async onLogout() {
    await this.auth.logout();
  }
}
