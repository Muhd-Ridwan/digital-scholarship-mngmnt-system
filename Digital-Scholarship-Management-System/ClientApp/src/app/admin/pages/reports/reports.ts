import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ToastService } from '../../../shared/services/toast.service';
import {
  ReportStat,
  StatusVolume,
  ScholarshipReportRow,
  ScreeningSummary,
} from '../../../shared/models/report.model';
import { ReportsService } from '../../../shared/services/api/reports.service';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [StatCard, LucideAngularModule],
  templateUrl: './reports.html',
})
export class AdminReports {
  private readonly toastService = inject(ToastService);
  private readonly reportsApi = inject(ReportsService);

  protected readonly stats = signal<ReportStat[]>([]);
  protected readonly byStatus = signal<StatusVolume[]>([]);
  protected readonly byScholarship = signal<ScholarshipReportRow[]>([]);
  protected readonly screening = signal<ScreeningSummary | null>(null);

  constructor() {
    // MOCK read now; becomes real HTTP GETs once the backend endpoints are available.
    this.reportsApi.getStats().subscribe((list) => this.stats.set(list));
    this.reportsApi.getApplicationsByStatus().subscribe((list) => this.byStatus.set(list));
    this.reportsApi.getByScholarship().subscribe((list) => this.byScholarship.set(list));
    this.reportsApi.getScreeningSummary().subscribe((s) => this.screening.set(s));
  }

  protected exportReport(): void {
    this.toastService.success('Report exported as CSV');
  }
}
