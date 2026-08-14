import { Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import { ReviewQueue, ReviewQueueItem } from '../../components/review-queue/review-queue';
import { AuthService } from '../../../auth/services/auth.service';
import { OfficerApplicationService } from '../../services/officer-application.service';

@Component({
  selector: 'app-officer-dashboard',
  standalone: true,
  imports: [
    DashboardHeader,
    StatCard,
    ActionCard,
    ReviewQueue,
    LucideAngularModule
  ],
  templateUrl: './officer-dashboard.html',
})
export class OfficerDashboard {

  private readonly auth = inject(AuthService);
  private readonly applicationService =
    inject(OfficerApplicationService);

  readonly profile = this.auth.profile;

  protected reviewQueue: ReviewQueueItem[] = [];

  protected scholarshipsAvailable = 0;
  protected assignedToYou = 0;
  protected reviewedThisWeek = 0;
  protected approvalRate = 0;


  async ngOnInit(): Promise<void> {
    await this.loadReviewQueue();
  }


  // ============================================================
  // LOAD REVIEW QUEUE
  // ============================================================

  async loadReviewQueue(): Promise<void> {

    try {

      const applications =
        await this.applicationService.getAllApplications();


         // Show applications that still require
      // officer attention.
      const relevantApplications =
        applications.filter(application =>
          application.status === 'pending' ||
          application.status === 'under_review' ||
          application.status === 'shortlisted'
        );


      // Convert backend application data
      // into the format expected by ReviewQueue.
      this.reviewQueue =
        relevantApplications.map(application => ({
          id: application.id.toString(),

          applicantName:
            application.studentName,

          scholarshipName:
            application.scholarshipTitle,

          submittedAt:
            this.formatSubmittedAt(
              application.submittedAt
            ),

          status:
            application.status
        }));


    } catch (error) {

      console.error(
        'Failed to load review queue:',
        error
      );

    }
  }


  // ============================================================
  // FORMAT SUBMISSION TIME
  // ============================================================

  private formatSubmittedAt(
    dateString: string
  ): string {

    const submitted =
      new Date(dateString);

    const now =
      new Date();

    const difference =
      now.getTime() -
      submitted.getTime();


    const minutes =
      Math.floor(
        difference / (1000 * 60)
      );

    const hours =
      Math.floor(minutes / 60);

    const days =
      Math.floor(hours / 24);


    if (minutes < 1) {
      return 'just now';
    }

    if (minutes < 60) {
      return `${minutes} minute${
        minutes === 1 ? '' : 's'
      } ago`;
    }

    if (hours < 24) {
      return `${hours} hour${
        hours === 1 ? '' : 's'
      } ago`;
    }

    return `${days} day${
      days === 1 ? '' : 's'
    } ago`;
  }


  // ============================================================
  // PRIORITY
  // ============================================================

  private getPriority(
    application: any
  ): 'urgent' | 'normal' {

    if (application.status === 'pending') {
      return 'urgent';
    }

    return 'normal';
  }
}