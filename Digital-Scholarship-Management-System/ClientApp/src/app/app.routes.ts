import { Routes } from '@angular/router';
import { DashboardShell } from './shared/components/dashboard-shell/dashboard-shell';
import { OfficerDashboard } from './officer/pages/dashboard/officer-dashboard';
import { SponsorDashboard } from './sponsor/pages/dashboard/sponsor-dashboard';
import { Login } from './auth/pages/login/login';
import { authGuard } from './auth/guards/auth.guards';
import { Register } from './auth/pages/register/register';
import { PublicShell } from './shared/components/public-shell/public-shell';
import { ScholarshipList } from './scholarships/pages/list/scholarship-list';
import { ScholarshipDetail } from './scholarships/pages/detail/scholarship-detail';
import { ForgotPassword } from './auth/pages/forgot-password/forgot-password';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'register', component: Register },
  {
    path: '',
    component: PublicShell,
    children: [
      { path: 'scholarships', component: ScholarshipList },
      { path: 'scholarships/:id', component: ScholarshipDetail },
      { path: '', redirectTo: 'scholarships', pathMatch: 'full' },
    ],
  },
  {
    path: '',
    component: DashboardShell,
    canActivate: [authGuard],
    children: [
      {
        path: 'student',
        canActivateChild: [authGuard],
        data: {
          role: 'user',
        },
        loadChildren: () => import('./student/student.routes').then((m) => m.STUDENT_ROUTES),
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
