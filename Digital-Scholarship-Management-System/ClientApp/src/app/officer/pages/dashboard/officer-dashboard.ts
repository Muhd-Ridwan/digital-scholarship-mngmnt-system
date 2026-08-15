import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import { ReviewQueue, ReviewQueueItem } from '../../components/review-queue/review-queue';
import { AuthService } from '../../../auth/services/auth.service';
import { OfficerApplicationService } from '../../services/officer-application.service';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';

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

  private readonly scholarshipService = inject(ScholarshipService);

  readonly profile = this.auth.profile;

  protected readonly reviewQueue = signal<ReviewQueueItem[]>([]);

  protected readonly scholarshipsAvailable = signal(0);
  protected readonly assignedToYou = signal(0);
  protected readonly reviewedThisWeek = signal(0);
  protected readonly approvalRate = signal(0);


  async ngOnInit(): Promise<void> {
    await this.loadReviewQueue();
    await this.loadScholarshipsAvailable();
    await this.loadAssignedToYou();
    await this.loadReviewedThisWeek();
    await this.loadApprovalRate();
  }

  //=============================================================
  //SCHOLARSHIPS AVAILABLE
  //=============================================================
    async loadScholarshipsAvailable(): Promise<void> {

    try {

      const scholarships =
        await this.scholarshipService.getAll();

      const now = new Date();

      const available =
        scholarships.filter(
          scholarship =>
            new Date(scholarship.deadline) > now
        );

      this.scholarshipsAvailable.set(
        available.length
      );

    } catch (error) {

      console.error(
        'Failed to load available scholarships:',
        error
      );

    }
  }

  //=============================================================
  // ASSIGNED TO ME (like number of pending applications)
  //=============================================================
  async loadAssignedToYou(): Promise<void> {

    try {

      const applications =
        await this.applicationService.getAllApplications();

      const assigned =
        applications.filter(application =>
          application.status === 'pending' ||
          application.status === 'under_review'
        );

      this.assignedToYou.set(assigned.length);

    } catch (error) {

      console.error(
        'Failed to load assigned applications:',
        error
      );

    }
  }

  //=============================================================
  // REVIEWED THIS WEEK
  //=============================================================

  async loadReviewedThisWeek(): Promise<void> {

    try {

      const applications =
        await this.applicationService.getAllApplications();

      const now = new Date();

      // Get the start of the current week (Monday)
      const startOfWeek = new Date(now);
      const day = startOfWeek.getDay();

      const daysFromMonday =
        day === 0 ? 6 : day - 1;

      startOfWeek.setDate(
        startOfWeek.getDate() - daysFromMonday
      );

      startOfWeek.setHours(0, 0, 0, 0);

      const reviewed =
        applications.filter(application => {

          if (!application.decisionAt) {
            return false;
          }

          const decisionDate =
            new Date(application.decisionAt);

          return decisionDate >= startOfWeek &&
                decisionDate <= now;
        });

      this.reviewedThisWeek.set(
        reviewed.length
      );

    } catch (error) {

      console.error(
        'Failed to load reviewed applications:',
        error
      );

    }
  }

  //=============================================================
  // APPROVAL RATE % BAR
  //=============================================================
  async loadApprovalRate(): Promise<void> {

    try {

      const applications =
        await this.applicationService.getAllApplications();

      const approved =
        applications.filter(
          application =>
            application.status === 'approved'
        ).length;

      const rejected =
        applications.filter(
          application =>
            application.status === 'rejected'
        ).length;

      const totalDecisions =
        approved + rejected;

      if (totalDecisions === 0) {

        this.approvalRate.set(0);

        return;
      }

      const rate =
        Math.round(
          (approved / totalDecisions) * 100
        );

      this.approvalRate.set(rate);

    } catch (error) {

      console.error(
        'Failed to calculate approval rate:',
        error
      );

    }
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
      this.reviewQueue.set(
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
        })));

        console.log('review queue: ', this.reviewQueue)


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

  
}