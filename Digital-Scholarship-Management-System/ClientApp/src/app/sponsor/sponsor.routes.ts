import { Routes } from '@angular/router';
import { SponsorDashboard } from './pages/dashboard/sponsor-dashboard';
import { SponsorScholarships } from './pages/scholarships/sponsor-scholarships';
import { SponsorScholarshipForm } from './pages/form/sponsor-scholarship-form';
import { SponsorScholarshipDetail } from './pages/detail/sponsor-scholarship-detail';
import { SponsorDisbursements } from './pages/disbursements/sponsor-disbursements';

export const SPONSOR_ROUTES: Routes = [
    { path: '', component: SponsorDashboard },
    { path: 'scholarships', component: SponsorScholarships },
    { path: 'scholarships/new', component: SponsorScholarshipForm },
    { path: 'scholarships/:id', component: SponsorScholarshipDetail },
    { path: 'disbursements', component: SponsorDisbursements },
];