import { Routes } from '@angular/router';
import { AdminDashboard } from './pages/dashboard/admin-dashboard';
import { AdminUsers } from './pages/users/users';
import { AdminAuditLog } from './pages/audit-log/audit-log';
import { AdminReports } from './pages/reports/reports';
import { AdminReferenceData } from './pages/reference-data/reference-data';
import { AdminAnnouncements } from './pages/announcements/announcements';

export const ADMIN_ROUTES: Routes = [
  { path: '', component: AdminDashboard },
  { path: 'users', component: AdminUsers },
  { path: 'audit-log', component: AdminAuditLog },
  { path: 'reports', component: AdminReports },
  { path: 'reference-data', component: AdminReferenceData },
  { path: 'announcements', component: AdminAnnouncements },
];
