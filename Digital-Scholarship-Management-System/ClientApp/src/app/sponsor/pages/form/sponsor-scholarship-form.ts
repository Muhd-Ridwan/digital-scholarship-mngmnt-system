import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
selector: 'app-sponsor-scholarship-form',
standalone: true,
imports: [ReactiveFormsModule],
templateUrl: './sponsor-scholarship-form.html',
})
export class SponsorScholarshipForm {
    private readonly fb = inject(FormBuilder);
    private readonly scholarshipsApi = inject(ScholarshipService);
    private readonly router = inject(Router);
    private readonly toast = inject(ToastService);

    readonly form = this.fb.nonNullable.group({
        title: ['', [Validators.required]],
        description: ['', [Validators.required]],
        eligibilityCriteria: ['', [Validators.required]],
        fundType: ['', [Validators.required]],
        studyLocation: ['', [Validators.required]],
        organisationType: ['', [Validators.required]],
        fundingAmount: [0, [Validators.required, Validators.min(0.01)]],
        deadline: ['', [Validators.required]],
    });

    readonly submitting = signal(false);

    async onSubmit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        this.submitting.set(true);
        try {
            await this.scholarshipsApi.create(this.form.getRawValue());
            this.toast.success('Scholarship created as a draft.');
            await this.router.navigateByUrl('/sponsor/scholarships');
        } catch {
            this.toast.error('Could not create this scholarship.');
        } finally {
            this.submitting.set(false);
        }
    }
}