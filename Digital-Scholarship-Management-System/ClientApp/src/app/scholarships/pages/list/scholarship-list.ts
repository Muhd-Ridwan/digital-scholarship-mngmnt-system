import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ScholarshipService } from '../../services/scholarship.service';
import { ScholarshipSummary } from '../../models/scholarship.model';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-scholarship-list',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe],
  templateUrl: './scholarship-list.html',
})
export class ScholarshipList {
  private readonly scholarshipService = inject(ScholarshipService);
  private readonly toast = inject(ToastService);

  readonly scholarships = signal<ScholarshipSummary[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.loadScholarships();
  }

  private async loadScholarships() {
    try {
      const scholarships = await this.scholarshipService.getAll();
      this.scholarships.set(scholarships);
    } catch {
      this.toast.error('Could not load scholarships. Please try again later.');
    } finally {
      this.loading.set(false);
    }
  }
}
