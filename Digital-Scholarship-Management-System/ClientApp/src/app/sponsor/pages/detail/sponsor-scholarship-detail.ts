import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { SponsorScholarshipDetail as SponsorScholarshipDetailModel } from '../../../scholarships/models/scholarship.model';
import { ToastService } from '../../../shared/services/toast.service';
import { STATUS_LABELS, STATUS_CLASSES } from '../../components/sponsor-listings/sponsor-listings';
import { LucideAngularModule } from 'lucide-angular';

@Component({
    selector: 'app-sponsor-scholarship-detail',
    standalone: true,
    imports: [RouterLink, DatePipe, CurrencyPipe, LucideAngularModule],
    templateUrl: './sponsor-scholarship-detail.html',
})
export class SponsorScholarshipDetail {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly scholarshipsApi = inject(ScholarshipService);
    private readonly toast = inject(ToastService);

    readonly scholarship = signal<SponsorScholarshipDetailModel | null>(null);
    readonly loading = signal(true);
    readonly notFound = signal(false);

    protected readonly statusLabel = (s: SponsorScholarshipDetailModel['status']) => STATUS_LABELS[s];
    protected readonly statusClasses = (s: SponsorScholarshipDetailModel['status']) => STATUS_CLASSES[s];

    constructor() {
        this.loadScholarship();
    }

    private async loadScholarship() {
        const id = Number(this.route.snapshot.paramMap.get('id'));
        try {
            const scholarship = await this.scholarshipsApi.getMineById(id);
            this.scholarship.set(scholarship);
        } catch (err: any) {
        if (err?.status === 404) {
            this.notFound.set(true);
        } else {
            this.toast.error('Could not load this scholarship.');
        }
        } finally {
            this.loading.set(false);
        }
    }

    protected async onPublish() {
        const s = this.scholarship();
        if (!s) return;
        try {
            await this.scholarshipsApi.publish(s.id);
            this.toast.success('Scholarship published.');
            await this.loadScholarship();
        } catch {
            this.toast.error('Could not publish this scholarship.');
        }
    }

    protected async onClose() {
        const s = this.scholarship();
        if (!s) return;
        try {
            await this.scholarshipsApi.close(s.id);
            this.toast.success('Scholarship closed.');
            await this.loadScholarship();
        } catch {
            this.toast.error('Could not close this scholarship.');
        }
    }

    protected async onDelete() {
        const s = this.scholarship();
        if (!s || !confirm('Delete this scholarship? This cannot be undone.')) return;
        try {
            await this.scholarshipsApi.delete(s.id);
            this.toast.success('Scholarship deleted.');
        await this.router.navigateByUrl('/sponsor/scholarships');
        } catch {
            this.toast.error('Could not delete this scholarship.');
        }
    }
}