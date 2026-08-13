import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { OfficerApplicationService } from '../../services/officer-application.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-application-details',
  standalone: true,
  imports: [
    CommonModule,
  ],
  templateUrl: './application-details.html'
})
export class ApplicationDetailsComponent implements OnInit {

  private readonly applicationService = inject(OfficerApplicationService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly application = signal<any | null>(null);

  ngOnInit(): void {
    const applicationId = Number(
      this.route.snapshot.paramMap.get('id')
    );

    if (!applicationId) {
      this.toast.error('Invalid application ID.');
      this.router.navigate(['/officer/applications']);
      return;
    }

    this.loadApplication(applicationId);
  }

  async loadApplication(id: number): Promise<void> {
    try {
      this.loading.set(true);

      const application =
        await this.applicationService.getApplication(id);

      this.application.set(application);

    } catch (error) {
      console.error('Failed to load application:', error);
      this.toast.error('Could not load application.');
    } finally {
      this.loading.set(false);
    }
  }

  async viewDocument(documentId: number): Promise<void> {
    try {
      const url =
        await this.applicationService.getDocumentDownloadUrl(documentId);

      window.open(url, '_blank');

    } catch (error) {
      console.error('Failed to open document:', error);
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

  async makeDecision(status: 'Shortlisted' | 'Rejected'): Promise<void> {
    if (!this.application()) {
      return;
    }

    const applicationId = this.application().id;

    const confirmed = window.confirm(
      status === 'Shortlisted'
        ? 'Are you sure you want to shortlist this application?'
        : 'Are you sure you want to reject this application?'
    );

    if (!confirmed) {
      return;
    }

    try {
      await this.applicationService.makeDecision(
        applicationId,
        status
      );

      this.application.update(app => ({
        ...app,
        status: status === 'Shortlisted'
          ? 'shortlisted'
          : 'rejected'
      }));

      this.toast.success(
        status === 'Shortlisted'
          ? 'Application shortlisted.'
          : 'Application rejected.'
      );

    } catch (error) {
      console.error('Failed to update application status:', error);

      this.toast.error(
        'Could not update application status.'
      );
    }
  }

  async undoDecision(): Promise<void> {
    if (!this.application()) {
      return;
    }

    const applicationId = this.application().id;

    const confirmed = window.confirm(
      'Undo this decision and return the application to Under Review?'
    );

    if (!confirmed) {
      return;
    }

    try {
      await this.applicationService.undoDecision(applicationId);

      this.application.update(app => ({
        ...app,
        status: 'under_review'
      }));

      this.toast.success(
        'Decision undone. Application is now under review.'
      );

    } catch (error) {
      console.error('Failed to undo application decision:', error);

      this.toast.error(
        'Could not undo the application decision.'
      );
    }
  }

  async approveApplication(): Promise<void> {
    const currentApplication = this.application();

    if (!currentApplication) {
      return;
    }

    try {
      await this.applicationService.reviewApplication(
        currentApplication.id,
        'Approved'
      );

      this.toast.success('Application approved.');

      await this.loadApplication(currentApplication.id);

    } catch (error) {
      console.error('Failed to approve application:', error);
      this.toast.error('Could not approve application.');
    }
  }

  async rejectApplication(): Promise<void> {
    const currentApplication = this.application();

    if (!currentApplication) {
      return;
    }

    try {
      await this.applicationService.reviewApplication(
        currentApplication.id,
        'Rejected'
      );

      this.toast.success('Application rejected.');

      await this.loadApplication(currentApplication.id);

    } catch (error) {
      console.error('Failed to reject application:', error);
      this.toast.error('Could not reject application.');
    }
  }

  scheduleInterview(): void {
    const currentApplication = this.application();

    if (!currentApplication) {
      return;
    }

    this.router.navigate([
      '/officer/interviews',
      currentApplication.id
    ]);
  }
}