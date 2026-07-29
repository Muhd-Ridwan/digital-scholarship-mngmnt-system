import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ScholarshipService } from '../../services/scholarship.service';
import { ScholarshipSummary } from '../../models/scholarship.model';
import { ToastService } from '../../../shared/services/toast.service';
import { AuthService } from '../../../auth/services/auth.service';
import { ROLE_ROUTES } from '../../../auth/role-routes';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-scholarship-list',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, LucideAngularModule],
  templateUrl: './scholarship-list.html',
})
export class ScholarshipList {
  private readonly scholarshipService = inject(ScholarshipService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);

  readonly scholarships = signal<ScholarshipSummary[]>([]);
  readonly loading = signal(true);

  readonly dashboardRoute = computed(() => {
    const p = this.auth.profile();
    return p ? ROLE_ROUTES[p.role] : null;
  });

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
