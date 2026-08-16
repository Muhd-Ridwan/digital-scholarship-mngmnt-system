import { Component, computed, inject, signal } from '@angular/core';
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
import { Scholarship } from '../../../scholarships/models/scholarship.model';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-sponsor-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, SponsorListings, LucideAngularModule, CurrencyPipe],
  templateUrl: './sponsor-dashboard.html',
})
export class SponsorDashboard {
  private readonly auth = inject(AuthService);
  private readonly scholarshipsApi = inject(ScholarshipService);

  readonly profile = this.auth.profile;

  protected readonly listings = signal<SponsorListingItem[]>([]);
  private readonly scholarships = signal<Scholarship[]>([]);

  protected readonly openCount = computed(
    () => this.scholarships().filter((s) => s.status === 'open').length,
  );
  protected readonly closedCount = computed(
    () => this.scholarships().filter((s) => s.status === 'closed').length,
  );
  protected readonly totalApplicants = computed(
    () => this.scholarships().reduce((sum, s) => sum + s.applications, 0),
  );
  protected readonly totalFunded = computed(
    () => this.scholarships().reduce((sum, s) => sum + s.disbursed, 0),
  );

  constructor() {
    this.scholarshipsApi.getMine().then((scholarships) => {
      this.scholarships.set(scholarships);
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