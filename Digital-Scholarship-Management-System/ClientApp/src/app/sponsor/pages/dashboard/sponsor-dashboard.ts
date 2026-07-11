import { Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  SponsorListings,
  SponsorListingItem,
} from '../../components/sponsor-listings/sponsor-listings';

@Component({
  selector: 'app-sponsor-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, SponsorListings, LucideAngularModule],
  templateUrl: './sponsor-dashboard.html',
})
export class SponsorDashboard {
  protected readonly listings: SponsorListingItem[] = [
    { id: '1', name: 'Merit Excellence Award', status: 'open', applicantCount: 24 },
    { id: '2', name: 'STEM Futures Grant', status: 'open', applicantCount: 18 },
    { id: '3', name: 'Community Leadership Fund', status: 'closed', applicantCount: 5 },
  ];
}
