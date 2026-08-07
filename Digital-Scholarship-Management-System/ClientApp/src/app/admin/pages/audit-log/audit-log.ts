import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ROLE_BADGE_CLASSES, UserRole } from '../../../shared/models/user.model';
import { AuditEntry } from '../../../shared/models/audit.model';
import { AuditService } from '../../../shared/services/api/audit.service';

type DateRangeOption = 'today' | 'last7' | 'last30';

const DATE_RANGE_LABELS: Record<DateRangeOption, string> = {
  today: 'Today',
  last7: 'Last 7 days',
  last30: 'Last 30 days',
};

@Component({
  selector: 'app-admin-audit-log',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './audit-log.html',
})
export class AdminAuditLog {
  private readonly auditApi = inject(AuditService);

  protected readonly roles: ('All' | UserRole)[] = ['All', 'user', 'officer', 'sponsor', 'admin'];
  protected readonly dateRangeOptions: DateRangeOption[] = ['today', 'last7', 'last30'];
  protected readonly dateRangeLabels = DATE_RANGE_LABELS;

  protected readonly roleFilter = signal<'All' | UserRole>('All');
  protected readonly personFilter = signal('');
  protected readonly dateRange = signal<DateRangeOption>('today');
  protected readonly loading = signal(true);

  private readonly entries = signal<AuditEntry[]>([]);

  constructor() {
    this.refreshEntries();
  }

  // Local calendar days, not rolling 24-hour windows — "Last 7 days" is seven dates
  // including today. Sent as UTC instants; the API trims to the exact window.
  private computeFromTo(option: DateRangeOption): { from: string; to: string } {
    const from = new Date();
    const to = new Date();

    const daysBack = option === 'today' ? 0 : option === 'last7' ? 6 : 29;
    from.setDate(from.getDate() - daysBack);
    from.setHours(0, 0, 0, 0);
    to.setHours(23, 59, 59, 999);

    return { from: from.toISOString(), to: to.toISOString() };
  }

  // Entries are stored in UTC; the admin reads them in their own timezone.
  protected formatTimestamp(iso: string): string {
    const date = new Date(iso);
    return Number.isNaN(date.getTime())
      ? iso
      : date.toLocaleString('en-GB', {
          day: 'numeric',
          month: 'short',
          year: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
        });
  }

  private refreshEntries(): void {
    this.loading.set(true);
    const { from, to } = this.computeFromTo(this.dateRange());
    this.auditApi
      .getEntries({ from, to })
      .then((list) => this.entries.set(list))
      .finally(() => this.loading.set(false));
  }

  protected readonly filteredEntries = computed(() => {
    const role = this.roleFilter();
    const person = this.personFilter().trim().toLowerCase();
    return this.entries().filter((entry) => {
      const matchesRole = role === 'All' || entry.role === role;
      const matchesPerson = !person || entry.user.toLowerCase().includes(person);
      return matchesRole && matchesPerson;
    });
  });

  protected roleBadgeClasses(role: UserRole): string {
    return ROLE_BADGE_CLASSES[role];
  }

  protected onRoleFilterChange(value: string): void {
    this.roleFilter.set(value as 'All' | UserRole);
  }

  protected onPersonFilterChange(value: string): void {
    this.personFilter.set(value);
  }

  protected onDateRangeChange(value: string): void {
    this.dateRange.set(value as DateRangeOption);
    this.refreshEntries();
  }
}
