import { Routes } from '@angular/router';
import { OfficerProfile } from './pages/profile/officer-profile';
import { OfficerApplicationsComponent } from './pages/application/officer-application';
import { OfficerDashboard } from './pages/dashboard/officer-dashboard';
import { ApplicationDetailsComponent } from './pages/application/application-details';
import { ScheduleInterviewComponent } from './pages/interview/schedule-interview';

export const OFFICER_ROUTES: Routes = [
  { path: '', component: OfficerDashboard },
  { path: 'profile', component: OfficerProfile }, //for officer view profile
  { path: 'applications', component: OfficerApplicationsComponent },
  { path: 'applications/:id', component: ApplicationDetailsComponent},
  { path: 'interviews/schedule/:applicationId', component: ScheduleInterviewComponent },
]