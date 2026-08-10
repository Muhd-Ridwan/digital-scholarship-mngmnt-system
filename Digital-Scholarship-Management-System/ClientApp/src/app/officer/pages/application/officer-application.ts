import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

import { OfficerApplicationService } from '../../services/officer-application.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-officer-applications',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    LucideAngularModule
  ],
  templateUrl: './officer-application.html'
})
export class OfficerApplicationsComponent {

  private readonly applicationService = inject(OfficerApplicationService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly applications = signal<any[]>([]);

  constructor() {
    this.loadApplications();
  }

  async loadApplications() {
    try {
      const applications =
        await this.applicationService.getAllApplications();

      this.applications.set(applications);
    } catch (error) {
      console.error(error);
      this.toast.error('Could not load applications.');
    } finally {
      this.loading.set(false);
    }
  }

  viewApplication(id: number) {
    this.router.navigate(['/officer/applications', id]);
  }

  formatStatus(status: string): string {
    switch (status) {
      case 'pending':
        return 'Pending';

      case 'under_review':
        return 'Under Review';

      case 'shortlisted':
        return 'Shortlisted';

      case 'approved':
        return 'Approved';

      case 'rejected':
        return 'Rejected';

      default:
        return status;
    }
  }

    scheduleInterview(applicationId: number) {
    this.router.navigate([
      '/officer/interviews/schedule',
      applicationId
    ]);
  } 
}