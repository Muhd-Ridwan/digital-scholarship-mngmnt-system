import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { SponsorReportsService } from '../../services/reports.service';
import { SponsorReportSummary } from '../../models/sponsor-report.model';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
    selector: 'app-sponsor-reports',
    standalone: true,
    imports: [RouterLink, DatePipe, CurrencyPipe, LucideAngularModule],
    templateUrl: './sponsor-reports.html',
})
export class SponsorReports {
    private readonly reportsApi = inject(SponsorReportsService);
    private readonly toast = inject(ToastService);

    readonly summary = signal<SponsorReportSummary | null>(null);
    readonly loading = signal(true);

    constructor() {
        this.load();
    }

    private async load() {
        try {
            this.summary.set(await this.reportsApi.getSummary());
        } catch {
            this.toast.error('Could not load your sponsorship report.');
        } finally {
            this.loading.set(false);
        }
    }
}