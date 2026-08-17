import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import { AuthService } from '../../../auth/services/auth.service';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { UsersService } from '../../../shared/services/api/users.service';
import { ReportsService } from '../../../shared/services/api/reports.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, LucideAngularModule],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly scholarshipsApi = inject(ScholarshipService);
  private readonly usersApi = inject(UsersService);
  private readonly reportsApi = inject(ReportsService);

  readonly profile = this.auth.profile;

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

  // Queue depth on the Sponsor Approvals card.
  protected readonly pendingSponsors = signal<number | null>(null);

  private readonly pendingReviewCount = signal<number | null>(null);
  protected readonly pendingReviewLabel = computed(() => {
    const count = this.pendingReviewCount();
    return count === null ? '…' : String(count);
  });

  private readonly totalAwardedCount = signal<number | null>(null);
  protected readonly totalAwardedLabel = computed(() => {
    const count = this.totalAwardedCount();
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

    this.usersApi.getSponsors().then((sponsors) => {
      const pending = sponsors.filter((s) => s.status === 'Pending').length;
      this.pendingSponsors.set(pending || null);
    });

    this.reportsApi.getSummary().then((summary) => {
      this.pendingReviewCount.set(summary.totals.pending);
      this.totalAwardedCount.set(summary.totals.approved);
    });
  }

  // Oversight, not a worklist — deciding applications belongs to the Officer.
  protected openReports(): void {
    void this.router.navigate(['/admin/reports']);
  }
}
