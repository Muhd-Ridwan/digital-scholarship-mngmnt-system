import { Routes } from '@angular/router';
import { StudentDashboard } from './pages/dashboard/student-dashboard';
import { StudentProfile } from './pages/profile/student-profile';
import { StudentDocuments } from './pages/documents/student-documents';
import { ScholarshipApply } from './pages/apply/scholarship-apply';
import { StudentApplications } from './pages/applications/student-applications';

export const STUDENT_ROUTES: Routes = [
  { path: '', component: StudentDashboard },
  { path: 'profile', component: StudentProfile },
  { path: 'documents', component: StudentDocuments },
  { path: 'apply/:scholarshipId', component: ScholarshipApply },
  { path: 'applications', component: StudentApplications },
];
