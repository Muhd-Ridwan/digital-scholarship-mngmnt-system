import { Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  ManagementPanel,
  ManagementItem,
} from '../../components/management-panel/management-panel';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, ManagementPanel, LucideAngularModule],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard {
  private readonly auth = inject(AuthService);

  readonly profile = this.auth.profile;
  // AD2 — global reference data & announcements.
  protected readonly managementItems: ManagementItem[] = [
    { label: 'Reference Data', icon: 'list-checks', route: '/admin/reference-data' },
    { label: 'Announcements', icon: 'bell', route: '/admin/announcements' },
  ];
}
