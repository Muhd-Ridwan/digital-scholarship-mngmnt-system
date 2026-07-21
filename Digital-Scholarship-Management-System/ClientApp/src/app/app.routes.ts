import { Routes } from '@angular/router';
import { DashboardShell } from './shared/components/dashboard-shell/dashboard-shell';
import { StudentDashboard } from './student/pages/dashboard/student-dashboard';
import { OfficerDashboard } from './officer/pages/dashboard/officer-dashboard';
import { SponsorDashboard } from './sponsor/pages/dashboard/sponsor-dashboard';
import { Login } from './auth/pages/login/login';
import { authGuard } from './auth/guards/auth.guards';
import { Register } from './auth/pages/register/register';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: '',
    component: DashboardShell,
    canActivate: [authGuard],
    children: [
      {
        path: 'student',
        component: StudentDashboard,
        canActivate: [authGuard],
        data: {
          role: 'user',
        },
      },
      {
        path: 'admin',
        canActivateChild: [authGuard],
        data: { role: 'admin' },
        loadChildren: () => import('./admin/admin.routes').then((m) => m.ADMIN_ROUTES),
      },
      {
        path: 'officer',
        component: OfficerDashboard,
        canActivate: [authGuard],
        data: {
          role: 'officer',
        },
      },
      {
        path: 'sponsor',
        component: SponsorDashboard,
        canActivate: [authGuard],
        data: {
          role: 'sponsor',
        },
      },
      { path: '', redirectTo: 'student', pathMatch: 'full' },
    ],
  },
];
