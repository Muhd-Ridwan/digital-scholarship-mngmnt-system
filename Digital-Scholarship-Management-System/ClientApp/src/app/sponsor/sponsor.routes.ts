import { Routes } from '@angular/router';
import { SponsorDashboard } from './pages/dashboard/sponsor-dashboard';
//   import { SponsorScholarships } from './pages/scholarships/sponsor-scholarships';   // the full list page
//   import { SponsorScholarshipForm } from './pages/scholarship-form/sponsor-scholarship-form'; // create/edit

  export const SPONSOR_ROUTES: Routes = [
    { path: '', component: SponsorDashboard },
    // { path: 'scholarships', component: SponsorScholarships },
    // { path: 'scholarships/new', component: SponsorScholarshipForm },
    // { path: 'scholarships/:id', component: SponsorScholarshipForm },
  ];