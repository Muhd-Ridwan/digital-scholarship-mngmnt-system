import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  ManagementPanel,
  ManagementItem,
} from '../../components/management-panel/management-panel';
import { AuthService } from '../../../auth/services/auth.service';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { UsersService } from '../../../shared/services/api/users.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, ManagementPanel, LucideAngularModule],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard {
  private readonly auth = inject(AuthService);
  private readonly scholarshipsApi = inject(ScholarshipService);
  private readonly usersApi = inject(UsersService);

  readonly profile = this.auth.profile;
  //global reference data & announcements
  protected readonly managementItems: ManagementItem[] = [
    { label: 'Reference Data', icon: 'list-checks', route: '/admin/reference-data' },
    { label: 'Announcements', icon: 'bell', route: '/admin/announcements' },
  ];

  // Scholarships Available — count where deadline >= today (GET /api/scholarships).
  private readonly scholarshipsAvailableCount = signal<number | null>(null);
  protected readonly scholarshipsAvailableLabel = computed(() => {
    const count = this.scholarshipsAvailableCount();
    return count === null ? '…' : String(count);
  });

  // Active Accounts — count where status = Active, i.e. not locked (GET /api/users).
  private readonly activeAccountsCount = signal<number | null>(null);
  protected readonly activeAccountsLabel = computed(() => {
    const count = this.activeAccountsCount();
    return count === null ? '…' : String(count);
  });

  constructor() {
    this.scholarshipsApi.getAll().then((scholarships) => {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const openCount = scholarships.filter((s) => new Date(s.deadline) >= today).length;
      this.scholarshipsAvailableCount.set(openCount);
    });

    this.usersApi.getUsers().then((users) => {
      const activeCount = users.filter((u) => u.status === 'Active').length;
      this.activeAccountsCount.set(activeCount);
    });
  }
}
