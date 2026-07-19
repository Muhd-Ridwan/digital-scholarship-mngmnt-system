import { Routes } from '@angular/router';
import { DashboardShell } from './shared/components/dashboard-shell/dashboard-shell';
import { StudentDashboard } from './student/pages/dashboard/student-dashboard';
import { OfficerDashboard } from './officer/pages/dashboard/officer-dashboard';
import { SponsorDashboard } from './sponsor/pages/dashboard/sponsor-dashboard';

export const routes: Routes = [
  {
    path: '',
    component: DashboardShell,
    children: [
      { path: 'student', component: StudentDashboard },
      { path: 'admin', loadChildren: () => import('./admin/admin.routes').then((m) => m.ADMIN_ROUTES) },
      { path: 'officer', component: OfficerDashboard },
      { path: 'sponsor', component: SponsorDashboard },
      { path: '', redirectTo: 'student', pathMatch: 'full' },
    ],
  },
];
