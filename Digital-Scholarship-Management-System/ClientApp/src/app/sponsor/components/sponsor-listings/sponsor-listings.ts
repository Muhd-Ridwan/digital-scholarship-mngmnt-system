import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

export type ListingStatus = 'open' | 'closed';

export interface SponsorListingItem {
  id: string;
  name: string;
  status: ListingStatus;
  applicantCount: number;
}

const STATUS_LABELS: Record<ListingStatus, string> = {
  open: 'Open',
  closed: 'Closed',
};

const STATUS_CLASSES: Record<ListingStatus, string> = {
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

  protected statusLabel(status: ListingStatus): string {
    return STATUS_LABELS[status];
  }

  protected statusClasses(status: ListingStatus): string {
    return STATUS_CLASSES[status];
  }
}
