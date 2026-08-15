import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../../../shared/services/toast.service';
import {
  SponsorDecision,
  SponsorProfile,
} from '../../../shared/models/sponsor-profile.model';
import { UsersService } from '../../../shared/services/api/users.service';

type RecordFilter = 'All' | 'Approved' | 'Rejected';

@Component({
  selector: 'app-admin-sponsors',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './sponsors.html',
})
export class AdminSponsors {
  private readonly toastService = inject(ToastService);
  private readonly usersApi = inject(UsersService);

  private readonly sponsors = signal<SponsorProfile[]>([]);
  // Without this an in-flight load renders the empty state, which reads as "no sponsors".
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly recordFilter = signal<RecordFilter>('All');
  protected readonly filters: RecordFilter[] = ['All', 'Approved', 'Rejected'];

  protected readonly pending = computed(() =>
    this.sponsors().filter((s) => s.status === 'Pending'),
  );
  protected readonly decided = computed(() =>
    this.sponsors().filter((s) => s.status !== 'Pending'),
  );

  protected readonly approvedCount = computed(
    () => this.decided().filter((r) => r.status === 'Approved').length,
  );
  protected readonly rejectedCount = computed(
    () => this.decided().filter((r) => r.status === 'Rejected').length,
  );

  protected readonly visibleRecord = computed(() => {
    const filter = this.recordFilter();
    const list = this.decided();
    return filter === 'All' ? list : list.filter((r) => r.status === filter);
  });

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.sponsors.set(await this.usersApi.getSponsors());
    } catch {
      this.toastService.error('Could not load sponsors.');
    } finally {
      this.loading.set(false);
    }
  }

  protected countFor(filter: RecordFilter): number {
    if (filter === 'Approved') return this.approvedCount();
    if (filter === 'Rejected') return this.rejectedCount();
    return this.decided().length;
  }

  protected formatDate(iso: string | null): string {
    if (!iso) return '—';
    const date = new Date(iso);
    return Number.isNaN(date.getTime())
      ? iso
      : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  protected badgeClass(status: SponsorDecision): string {
    switch (status) {
      case 'Approved':
        return 'bg-status-success/15 text-status-success';
      case 'Rejected':
        return 'bg-status-danger/15 text-status-danger';
      default:
        return 'bg-status-warning/15 text-status-warning';
    }
  }

  // The row moves from the queue to the record rather than disappearing — a refused
  // company still applied, and that is worth keeping.
  private replace(updated: SponsorProfile): void {
    this.sponsors.update((list) => list.map((s) => (s.id === updated.id ? updated : s)));
  }

  // The decision targets the sponsor account, not any listing it has posted.
  protected async approve(sponsor: SponsorProfile): Promise<void> {
    if (this.busy()) return;

    this.busy.set(true);
    try {
      this.replace(await this.usersApi.approveSponsor(sponsor.id));
      this.toastService.success(`${sponsor.companyName} approved — can now post scholarships`);
    } catch {
      this.toastService.error(`Could not approve ${sponsor.companyName}.`);
    } finally {
      this.busy.set(false);
    }
  }

  protected async reject(sponsor: SponsorProfile): Promise<void> {
    if (this.busy()) return;

    this.busy.set(true);
    try {
      this.replace(await this.usersApi.rejectSponsor(sponsor.id));
      this.toastService.error(`${sponsor.companyName} rejected — cannot post scholarships`);
    } catch {
      this.toastService.error(`Could not reject ${sponsor.companyName}.`);
    } finally {
      this.busy.set(false);
    }
  }
}
