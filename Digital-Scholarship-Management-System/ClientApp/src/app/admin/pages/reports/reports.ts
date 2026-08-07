import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ToastService } from '../../../shared/services/toast.service';
import {
  ApplicationStatus,
  ReferenceValue,
  ReportStat,
  ReportsSummary,
  ScholarshipState,
  StatusVolume,
} from '../../../shared/models/report.model';
import { ReportsService } from '../../../shared/services/api/reports.service';

type ReportTab = 'scholarships' | 'applications' | 'summary';
type ScholarshipFilter = 'All' | ScholarshipState;
type ApplicationFilter = 'All' | ApplicationStatus;

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [StatCard, LucideAngularModule],
  templateUrl: './reports.html',
})
export class AdminReports {
  private readonly toastService = inject(ToastService);
  private readonly reportsApi = inject(ReportsService);

  protected readonly tabs: { id: ReportTab; label: string }[] = [
    { id: 'scholarships', label: 'Scholarships' },
    { id: 'applications', label: 'Applications' },
    { id: 'summary', label: 'Summary' },
  ];
  protected readonly activeTab = signal<ReportTab>('scholarships');

  protected readonly scholarshipFilters: ScholarshipFilter[] = ['All', 'Open', 'Closed', 'Withdrawn'];
  protected readonly applicationFilters: ApplicationFilter[] = [
    'All',
    'Pending',
    'Under Review',
    'Approved',
    'Rejected',
  ];
  protected readonly scholarshipFilter = signal<ScholarshipFilter>('All');
  protected readonly applicationFilter = signal<ApplicationFilter>('All');

  private readonly summary = signal<ReportsSummary | null>(null);
  // Without this an in-flight load renders the empty state, which reads as no data.
  protected readonly loading = signal(true);

  protected readonly scholarships = computed(() => this.summary()?.scholarships ?? []);
  protected readonly applications = computed(() => this.summary()?.applications ?? []);
  protected readonly awardsByFundType = computed(() => this.summary()?.awardsByFundType ?? []);
  protected readonly valuesInUse = computed<ReferenceValue[]>(
    () => this.summary()?.valuesInUse ?? [],
  );

  protected readonly stats = computed<ReportStat[]>(() => {
    const totals = this.summary()?.totals;
    if (!totals) {
      return [];
    }
    const decided = totals.approved + totals.rejected;
    return [
      { label: 'Scholarships Posted', value: String(totals.scholarships), icon: 'graduation-cap', tone: 'gold', description: 'All listings, any state' },
      { label: 'Applications Received', value: String(totals.applications), icon: 'file-text', tone: 'gold', description: 'Across all scholarships' },
      { label: 'Total Awarded', value: String(totals.approved), icon: 'hand-coins', tone: 'success', description: 'Approved applications' },
      // Avoid dividing by zero when nothing has been decided.
      {
        label: 'Approval Rate',
        value: decided === 0 ? '—' : `${Math.round((totals.approved * 100) / decided)}%`,
        icon: 'circle-check',
        tone: 'success',
        description: 'Of decided applications',
      },
    ];
  });

  // Bar width is relative to the largest bucket, not to the total.
  protected readonly byStatus = computed<StatusVolume[]>(() => {
    const totals = this.summary()?.totals;
    if (!totals) {
      return [];
    }
    const rows: { status: ApplicationStatus; count: number }[] = [
      { status: 'Pending', count: totals.pending },
      { status: 'Under Review', count: totals.underReview },
      { status: 'Approved', count: totals.approved },
      { status: 'Rejected', count: totals.rejected },
    ];
    const largest = Math.max(...rows.map((row) => row.count), 1);
    return rows.map((row) => ({ ...row, percent: Math.round((row.count * 100) / largest) }));
  });

  protected readonly visibleScholarships = computed(() => {
    const filter = this.scholarshipFilter();
    const list = this.scholarships();
    return filter === 'All' ? list : list.filter((s) => s.status === filter);
  });

  protected readonly visibleApplications = computed(() => {
    const filter = this.applicationFilter();
    const list = this.applications();
    return filter === 'All' ? list : list.filter((a) => a.status === filter);
  });

  protected readonly fundTypeValues = computed(() =>
    this.valuesInUse().filter((v) => v.category === 'FundType'),
  );
  protected readonly studyLocationValues = computed(() =>
    this.valuesInUse().filter((v) => v.category === 'StudyLocation'),
  );
  protected readonly organisationTypeValues = computed(() =>
    this.valuesInUse().filter((v) => v.category === 'OrganisationType'),
  );

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.summary.set(await this.reportsApi.getSummary());
    } catch {
      this.toastService.error('Could not load report data.');
    } finally {
      this.loading.set(false);
    }
  }

  protected setTab(tab: ReportTab): void {
    this.activeTab.set(tab);
  }

  protected formatDate(iso: string): string {
    const date = new Date(iso);
    return Number.isNaN(date.getTime())
      ? iso
      : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  // FundType is free text, so title-case it for display.
  protected displayFundType(value: string): string {
    return value ? value.charAt(0).toUpperCase() + value.slice(1).toLowerCase() : value;
  }

  protected scholarshipBadge(status: ScholarshipState): string {
    switch (status) {
      case 'Open':
        return 'bg-status-success/15 text-status-success';
      case 'Withdrawn':
        return 'bg-status-warning/15 text-status-warning';
      default:
        return 'bg-ink-800 text-mist-400';
    }
  }

  protected applicationBadge(status: ApplicationStatus): string {
    switch (status) {
      case 'Approved':
        return 'bg-status-success/15 text-status-success';
      case 'Rejected':
        return 'bg-status-danger/15 text-status-danger';
      case 'Pending':
        return 'bg-status-warning/15 text-status-warning';
      default:
        return 'bg-gold-500/15 text-gold-500';
    }
  }

  // Flags a value that differs from another only by case.
  protected isCaseVariant(value: ReferenceValue, group: ReferenceValue[]): boolean {
    return group.some(
      (other) =>
        other.value !== value.value &&
        other.value.toLowerCase() === value.value.toLowerCase() &&
        other.count > value.count,
    );
  }

  // Exports the tab that is open, using the rows the filters are currently showing.
  protected exportReport(): void {
    const tab = this.activeTab();
    let name: string;
    let rows: (string | number)[][];

    if (tab === 'scholarships') {
      name = 'scholarships';
      rows = [
        ['Scholarship', 'Sponsor', 'Fund type', 'Applications', 'Deadline', 'Status'],
        ...this.visibleScholarships().map((row) => [
          row.title,
          row.sponsor,
          row.fundType,
          row.applications,
          row.deadline,
          row.status,
        ]),
      ];
    } else if (tab === 'applications') {
      name = 'applications';
      rows = [
        ['Student', 'Scholarship', 'Submitted', 'Status'],
        ...this.visibleApplications().map((row) => [
          row.student,
          row.scholarship,
          row.submitted,
          row.status,
        ]),
      ];
    } else {
      name = 'summary';
      rows = [
        ['Metric', 'Value'],
        ...this.stats().map((stat) => [stat.label, stat.value]),
        [],
        ['Status', 'Count'],
        ...this.byStatus().map((row) => [row.status, row.count]),
        [],
        ['Fund type', 'Awards'],
        ...this.awardsByFundType().map((row) => [row.fundType, row.count]),
      ];
    }

    if (rows.length <= 1) {
      this.toastService.error('Nothing to export in this view.');
      return;
    }

    const today = new Date().toISOString().slice(0, 10);
    this.downloadCsv(`${name}-${today}.csv`, this.toCsv(rows));
    this.toastService.success('Report exported as CSV');
  }

  // Quote any cell holding a comma, quote or line break.
  private toCsv(rows: (string | number)[][]): string {
    return rows
      .map((row) =>
        row
          .map((cell) => {
            const text = String(cell ?? '');
            return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
          })
          .join(','),
      )
      .join('\r\n');
  }

  private downloadCsv(filename: string, csv: string): void {
    // The BOM makes Excel read the file as UTF-8.
    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
  }
}
