import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import { InterviewService } from '../../services/interview.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-shortlisted-applicants',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './shortlisted-applicants.html'
})
export class ShortlistedApplicantsComponent implements OnInit {

  private readonly interviewService = inject(InterviewService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);


  protected readonly loading = signal(true);
  protected readonly applicants = signal<any[]>([]);
  protected readonly upcomingInterviews = signal<any[]>([]);


  ngOnInit(): void {
    this.loadApplicants();
    this.loadUpcomingInterviews();
  }


  async loadApplicants(): Promise<void> {

    try {

      const applicants =
        await this.interviewService.getShortlistedApplicants();

      this.applicants.set(applicants);

    } catch (error) {

      console.error(
        'Failed to load shortlisted applicants:',
        error
      );

      this.toast.error(
        'Could not load shortlisted applicants.'
      );

    } finally {

      this.loading.set(false);

    }
  }


  async loadUpcomingInterviews(): Promise<void> {

    try {

      const interviews =
        await this.interviewService.getUpcomingInterviews();

      this.upcomingInterviews.set(interviews);

    } catch (error) {

      console.error(
        'Failed to load upcoming interviews:',
        error
      );

      this.toast.error(
        'Could not load upcoming interviews.'
      );

    }
  }


  scheduleInterview(applicationId: number): void {

    this.router.navigate([
      '/officer/interviews',
      applicationId
    ]);

  }

}