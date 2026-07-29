import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastService } from '../../../shared/services/toast.service';
import { ROLE_BADGE_CLASSES, User, UserRole, UserStatus } from '../../../shared/models/user.model';
import { SponsorProfile } from '../../../shared/models/sponsor-profile.model';
import { UsersService } from '../../../shared/services/api/users.service';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './users.html',
})
export class AdminUsers {
  private readonly toastService = inject(ToastService);
  private readonly usersApi = inject(UsersService);
  private readonly authApi = inject(AuthService);

  protected readonly pendingSponsors = signal<SponsorProfile[]>([]);
  protected readonly users = signal<User[]>([]);
  protected readonly loadingUsers = signal(true);
  protected readonly registering = signal(false);

  constructor() {
    this.usersApi.getPendingSponsors().subscribe((list) => this.pendingSponsors.set(list));
    this.refreshUsers();
  }

  private refreshUsers(): void {
    this.loadingUsers.set(true);
    this.usersApi
      .getUsers()
      .then((list) => this.users.set(list))
      .catch(() => this.toastService.error('Could not load users from the API.'))
      .finally(() => this.loadingUsers.set(false));
  }

  protected roleBadgeClasses(role: UserRole): string {
    return ROLE_BADGE_CLASSES[role];
  }

  protected viewCertificate(sponsor: SponsorProfile): void {
    this.toastService.success(
      `Opening business-registration certificate for ${sponsor.companyName}…`,
    );
  }

  protected approveSponsor(sponsor: SponsorProfile): void {
    this.pendingSponsors.update((list) => list.filter((s) => s.id !== sponsor.id));
    this.toastService.success(`${sponsor.companyName} approved — can now post scholarships`);
  }

  protected rejectSponsor(sponsor: SponsorProfile): void {
    this.pendingSponsors.update((list) => list.filter((s) => s.id !== sponsor.id));
    this.toastService.error(`${sponsor.companyName} onboarding rejected`);
  }

  protected async toggleLock(user: User): Promise<void> {
    const nextStatus: UserStatus = user.status === 'Active' ? 'Locked' : 'Active';
    try {
      const updated = await this.usersApi.setStatus(user.id, nextStatus);
      this.users.update((list) => list.map((u) => (u.id === user.id ? updated : u)));
      this.toastService.success(
        nextStatus === 'Locked' ? `Account locked: ${user.name}` : `Account unlocked: ${user.name}`,
      );
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        this.toastService.error('The Admin account cannot be locked.');
      } else {
        this.toastService.error(`Could not update status for ${user.name}.`);
      }
    }
  }

  protected async registerOfficer(
    username: string,
    fullName: string,
    email: string,
  ): Promise<boolean> {
    const uname = username.trim();
    const name = fullName.trim();
    const mail = email.trim();
    if (!uname || !name || !mail) {
      this.toastService.error('Username, full name, and email are required to register a reviewer');
      return false;
    }

    this.registering.set(true);
    try {
      await this.authApi.registerOfficer({ username: uname, fullName: name, email: mail });
      this.toastService.success(`Officer registered — temp password emailed to ${mail}`);
      this.refreshUsers();
      return true;
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 409) {
        this.toastService.error('That email is already registered.');
      } else if (err instanceof HttpErrorResponse && err.status === 403) {
        this.toastService.error('Only an Admin can register an Officer.');
      } else {
        this.toastService.error('Officer registration could not be completed.');
      }
      return false;
    } finally {
      this.registering.set(false);
    }
  }

  protected async onRegisterSubmit(
    usernameInput: HTMLInputElement,
    fullNameInput: HTMLInputElement,
    emailInput: HTMLInputElement,
  ): Promise<void> {
    const registered = await this.registerOfficer(
      usernameInput.value,
      fullNameInput.value,
      emailInput.value,
    );
    if (registered) {
      usernameInput.value = '';
      fullNameInput.value = '';
      emailInput.value = '';
    }
  }
}
