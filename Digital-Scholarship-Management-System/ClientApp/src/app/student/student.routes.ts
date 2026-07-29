import { Routes } from '@angular/router';
import { StudentDashboard } from './pages/dashboard/student-dashboard';
import { StudentProfile } from './pages/profile/student-profile';
import { StudentDocuments } from './pages/documents/student-documents';

export const STUDENT_ROUTES: Routes = [
  { path: '', component: StudentDashboard },
  { path: 'profile', component: StudentProfile },
  { path: 'documents', component: StudentDocuments },
];
