import { Routes } from '@angular/router';
import { AdminDashboard } from './pages/dashboard/admin-dashboard';
import { AdminUsers } from './pages/users/users';
import { AdminSponsors } from './pages/sponsors/sponsors';
import { AdminAuditLog } from './pages/audit-log/audit-log';
import { AdminReports } from './pages/reports/reports';
import { AdminAnnouncements } from './pages/announcements/announcements';
import { AdminProfile } from './pages/profile/profile';

export const ADMIN_ROUTES: Routes = [
  { path: '', component: AdminDashboard },
  { path: 'users', component: AdminUsers },
  { path: 'sponsors', component: AdminSponsors },
  { path: 'audit-log', component: AdminAuditLog },
  { path: 'reports', component: AdminReports },
  { path: 'announcements', component: AdminAnnouncements },
  { path: 'profile', component: AdminProfile },
];
