import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  SponsorListings,
  SponsorListingItem,
} from '../../components/sponsor-listings/sponsor-listings';
import { AuthService } from '../../../auth/services/auth.service';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';

@Component({
  selector: 'app-sponsor-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, SponsorListings, LucideAngularModule],
  templateUrl: './sponsor-dashboard.html',
})
export class SponsorDashboard {
  private readonly auth = inject(AuthService);

  private readonly scholarshipsApi = inject(ScholarshipService);

  readonly profile = this.auth.profile;

  protected readonly listings = signal<SponsorListingItem[]>([]);

  constructor() {
    this.scholarshipsApi.getMine().then((scholarships) => {
      this.listings.set(
        scholarships.map((s) => ({
          id: String(s.id),
          name: s.title,
          status: s.status,
          applicantCount: s.applications,
        })),
      );
    });
  }
}