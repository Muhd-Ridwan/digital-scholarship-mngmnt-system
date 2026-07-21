import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../../../shared/services/toast.service';
import { ROLE_BADGE_CLASSES, User, UserRole, UserStatus } from '../../../shared/models/user.model';
import { SponsorProfile } from '../../../shared/models/sponsor-profile.model';
import { UsersService } from '../../../shared/services/api/users.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './users.html',
})
export class AdminUsers {
  private readonly toastService = inject(ToastService);
  private readonly usersApi = inject(UsersService);

  protected readonly pendingSponsors = signal<SponsorProfile[]>([]);
  protected readonly users = signal<User[]>([]);

  constructor() {
    // MOCK read now; becomes a real HTTP GET once the backend endpoint is available.
    this.usersApi.getUsers().subscribe((list) => this.users.set(list));
    this.usersApi.getPendingSponsors().subscribe((list) => this.pendingSponsors.set(list));
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

  protected toggleLock(user: User): void {
    const nextStatus: UserStatus = user.status === 'Active' ? 'Locked' : 'Active';
    this.users.update((list) =>
      list.map((u) => (u.id === user.id ? { ...u, status: nextStatus } : u)),
    );
    this.toastService.success(
      nextStatus === 'Locked' ? `Account locked: ${user.name}` : `Account unlocked: ${user.name}`,
    );
  }

  protected registerOfficer(fullName: string, email: string): boolean {
    const name = fullName.trim();
    const mail = email.trim();
    if (!name || !mail) {
      this.toastService.error('Full name and email are required to register a reviewer');
      return false;
    }
    this.users.update((list) => [
      ...list,
      { id: `u-${Date.now()}`, name, email: mail, role: 'Officer', status: 'Active' },
    ]);
    this.toastService.success('Officer registered — Cognito group assigned');
    return true;
  }

  protected onRegisterSubmit(fullNameInput: HTMLInputElement, emailInput: HTMLInputElement): void {
    const registered = this.registerOfficer(fullNameInput.value, emailInput.value);
    if (registered) {
      fullNameInput.value = '';
      emailInput.value = '';
    }
  }
}
