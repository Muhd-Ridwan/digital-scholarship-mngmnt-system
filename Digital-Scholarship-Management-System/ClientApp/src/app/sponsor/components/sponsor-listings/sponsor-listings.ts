import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ScholarshipStatus } from '../../../scholarships/models/scholarship.model';

export interface SponsorListingItem {
  id: string;
  name: string;
  status: ScholarshipStatus;
  applicantCount: number;
}

export const STATUS_LABELS: Record<ScholarshipStatus, string> = {
  draft: 'Draft',
  open: 'Open',
  closed: 'Closed',
};

export const STATUS_CLASSES: Record<ScholarshipStatus, string> = {
  draft: 'bg-status-warning/10 text-status-warning',
  open: 'bg-status-success/10 text-status-success',
  closed: 'bg-mist-400/10 text-mist-400',
};

@Component({
  selector: 'app-sponsor-listings',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './sponsor-listings.html',
})
export class SponsorListings {
  readonly listings = input<SponsorListingItem[]>([]);
  readonly createRoute = input<string | null>(null);

  protected statusLabel(status: ScholarshipStatus): string {
    return STATUS_LABELS[status];
  }

  protected statusClasses(status: ScholarshipStatus): string {
    return STATUS_CLASSES[status];
  }
}
