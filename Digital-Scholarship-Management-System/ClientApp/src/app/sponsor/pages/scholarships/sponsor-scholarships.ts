import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { Scholarship, ScholarshipStatus } from '../../../scholarships/models/scholarship.model';
import { ToastService } from '../../../shared/services/toast.service';
import { STATUS_LABELS, STATUS_CLASSES } from '../../components/sponsor-listings/sponsor-listings'
import { CurrencyPipe, DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';


@Component({
selector: 'app-sponsor-scholarships',
standalone: true,
imports: [RouterLink, DatePipe, CurrencyPipe, LucideAngularModule],
templateUrl: './sponsor-scholarships.html',
})
export class SponsorScholarships {
    private readonly scholarshipsApi = inject(ScholarshipService);
    private readonly toast = inject(ToastService);

    readonly scholarships = signal<Scholarship[]>([]);
    readonly loading = signal(true);

    protected statusLabel(status: ScholarshipStatus): string { return STATUS_LABELS[status]; }
    protected statusClasses(status: ScholarshipStatus): string { return STATUS_CLASSES[status]; }

    constructor() {
        this.loadScholarships();
    }

    private async loadScholarships() {
        try {
            const scholarships = await this.scholarshipsApi.getMine();
            this.scholarships.set(scholarships);
        } catch {
            this.toast.error('Could not load your scholarships. Please try again later.');
        } finally {
            this.loading.set(false);
        }
    }

    

    protected async onPublish(id: number) {
        try {
            await this.scholarshipsApi.publish(id);
            this.toast.success('Scholarship published.');
            await this.loadScholarships();
        } catch {
            this.toast.error('Could not publish this scholarship.');
        }
    }

    protected async onClose(id: number) {
        try {
            await this.scholarshipsApi.close(id);
            this.toast.success('Scholarship closed.');
            await this.loadScholarships();
        } catch {
            this.toast.error('Could not close this scholarship.');
        }
    }

    protected async onDelete(id: number) {
        if (!confirm('Delete this scholarship? This cannot be undone.')) {
            return;
        }
        try {
            await this.scholarshipsApi.delete(id);
            this.toast.success('Scholarship deleted.');
            await this.loadScholarships();
        } catch {
            this.toast.error('Could not delete this scholarship.');
        }
    }
}