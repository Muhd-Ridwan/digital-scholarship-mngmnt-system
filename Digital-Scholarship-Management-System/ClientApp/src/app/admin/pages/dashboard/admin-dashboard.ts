import { Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  ManagementPanel,
  ManagementItem,
} from '../../components/management-panel/management-panel';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, ManagementPanel, LucideAngularModule],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard {
  protected readonly managementItems: ManagementItem[] = [
    { label: 'Manage Users', icon: 'users', route: '/admin/users' },
    { label: 'Quota Management', icon: 'target', route: '/admin/quota' },
    { label: 'Holidays', icon: 'calendar', route: '/admin/holidays' },
    { label: 'Roster Config', icon: 'settings', route: '/admin/roster-config' },
  ];
}
