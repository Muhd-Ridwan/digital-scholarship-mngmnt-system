import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ScholarshipService } from '../../services/scholarship.service';
import { ScholarshipDetail as ScholarshipDetailModel } from '../../models/scholarship.model';
import { ToastService } from '../../../shared/services/toast.service';
import { AuthService } from '../../../auth/services/auth.service';
import { LoginDialog } from '../../../shared/components/login-dialog/login-dialog';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-scholarship-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, LoginDialog, LucideAngularModule],
  templateUrl: './scholarship-detail.html',
})
export class ScholarshipDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly scholarshipService = inject(ScholarshipService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);

  readonly scholarship = signal<ScholarshipDetailModel | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly showLoginDialog = signal(false);

  constructor() {
    this.loadScholarship();
  }

  private async loadScholarship() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    try {
      const scholarship = await this.scholarshipService.getById(id);
      this.scholarship.set(scholarship);
    } catch (err: any) {
      if (err?.status === 404) {
        this.notFound.set(true);
      } else {
        this.toast.error('Could not load this scholarship. Please try again later.');
      }
    } finally {
      this.loading.set(false);
    }
  }
  async onApply() {
    const profile = await this.auth.getProfile(); // THis is because, the auth.getProfile doesnt return the profile directly, so need to use await and the onApply become async
    if (!profile) {
      this.showLoginDialog.set(true);
      return;
    }
    this.toast.success('Application flow coming soon!.');
  }
}
