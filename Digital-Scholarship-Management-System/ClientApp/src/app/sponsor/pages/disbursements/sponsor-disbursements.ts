import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ApplicationService } from '../../services/application.service';
import { PendingDisbursement } from '../../models/pending-disbursement.model';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
    selector: 'app-sponsor-disbursements',
    standalone: true,
    imports: [RouterLink, DatePipe, LucideAngularModule],
    templateUrl: './sponsor-disbursements.html',
})
export class SponsorDisbursements {
    private readonly applicationsApi = inject(ApplicationService);
    private readonly toast = inject(ToastService);

    readonly pending = signal<PendingDisbursement[]>([]);
    readonly loading = signal(true);
    readonly amounts = signal<Record<number, number | null>>({});

    constructor() {
        this.loadPending();
    }

    private async loadPending() {
        try {
            const pending = await this.applicationsApi.getPendingDisbursements();
            this.pending.set(pending);
            this.amounts.set(
                Object.fromEntries(pending.map((p) => [p.id, p.fundingAmount])),
            );
        } catch {
            this.toast.error('Could not load pending disbursements. Please try again later.');
        } finally {
            this.loading.set(false);
        }
    }

    protected amountFor(id: number): number | null {
        return this.amounts()[id] ?? null;
    }

    protected onAmountChange(id: number, value: string) {
        const amount = value === '' ? null : Number(value);
        this.amounts.update((current) => ({ ...current, [id]: amount }));
    }

    protected async onDisburse(id: number) {
        const amount = this.amountFor(id);
        if (!amount || amount <= 0) {
            this.toast.error('Enter a valid disbursement amount.');
            return;
        }
        try {
            await this.applicationsApi.disburse(id, amount);
            this.toast.success('Disbursement recorded.');
            await this.loadPending();
        } catch {
            this.toast.error('Could not record this disbursement.');
        }
    }
}