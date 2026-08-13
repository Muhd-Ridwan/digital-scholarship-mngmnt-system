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
    LucideAngularModule,
    
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

  // viewApplication(id: number): void {
  //   this.router.navigate(['/officer/applications', id]);
  // }

  // async viewApplication(id: number): Promise<void> {
  async viewApplication(application:any): Promise<void> {

      // Only show confirmation if the application is still Pending
    if (application.status === 'pending') {

    const confirmed = window.confirm(
      'Viewing this application will change its status to Under Review.\n\nDo you want to continue?'
    );

    if (!confirmed) {
      return;
    }

    try {

      await this.applicationService.startReview(application.id);

      this.toast.success('Application is now under review.');

    } catch (error) {

      console.error('Failed to start application review:', error);

      this.toast.error(
        'Could not change application status to Under Review.'
      );

      return;
    }
  }

  this.router.navigate([
        '/officer/applications',
        application.id
      ]);
  }

  async viewDocument(documentId: number) {
  try {
    const url = await this.applicationService.getDocumentDownloadUrl(documentId);

    window.open(url, '_blank');
  } catch (error) {
    console.error('Could not open document:', error);
    this.toast.error('Could not open document.');
  }
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