import { Routes } from '@angular/router';
import { OfficerProfile } from './pages/profile/officer-profile';
import { OfficerApplicationsComponent } from './pages/application/officer-application';
import { OfficerDashboard } from './pages/dashboard/officer-dashboard';
import { ApplicationDetailsComponent } from './pages/application/application-details';
import { ScheduleInterviewComponent } from './pages/interview/schedule-interview';
import { ShortlistedApplicantsComponent } from './pages/interview/shortlisted-applicants';
import { OfficerReports } from './pages/reports/officer-reports';


export const OFFICER_ROUTES: Routes = [
  { path: '', component: OfficerDashboard },
  { path: 'profile', component: OfficerProfile }, //for officer view profile
  { path: 'applications', component: OfficerApplicationsComponent },
  { path: 'applications/:id', component: ApplicationDetailsComponent},
  { path: 'interviews/:applicationId', component: ScheduleInterviewComponent },
  { path: 'interviews', component: ShortlistedApplicantsComponent },
  { path: 'reports', component: OfficerReports },
]