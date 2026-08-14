import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { RouterLink } from '@angular/router';

import { InterviewService } from '../../services/interview.service';
import { ToastService } from '../../../shared/services/toast.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-shortlisted-applicants',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, RouterLink, FormsModule],
  templateUrl: './shortlisted-applicants.html'
})
export class ShortlistedApplicantsComponent implements OnInit {

  private readonly interviewService = inject(InterviewService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);


  protected readonly loading = signal(true);
  protected readonly applicants = signal<any[]>([]);
  protected readonly upcomingInterviews = signal<any[]>([]);
  protected readonly pastInterviews = signal<any[]>([]);

  protected readonly showDecisionPopup = signal(false);
  protected readonly selectedApplication = signal<any | null>(null);

protected readonly selectedInterview =
  signal<any | null>(null);

protected readonly selectedDecision =
  signal<'approved' | 'rejected' | null>(null);

  confirmDecision(
  interview: any,
  decision: 'approved' | 'rejected'
  ): void {

    this.selectedInterview.set(interview);
    this.selectedDecision.set(decision);
    this.showDecisionPopup.set(true);

  }
  cancelDecision(): void {
  this.showDecisionPopup.set(false);
  this.selectedInterview.set(null);
  this.selectedDecision.set(null);
}


  ngOnInit(): void {
    this.loadApplicants();
    this.loadUpcomingInterviews();
    this.loadInterviews();
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

    async loadInterviews(): Promise<void> {

  try {

    const interviews =
      await this.interviewService.getAllInterviews();

    const now = new Date();

    const upcoming = interviews.filter(interview => {
      return this.getInterviewDateTime(interview) > now;
    });

    const past = interviews.filter(interview => {
      return this.getInterviewDateTime(interview) <= now;
    });

    this.upcomingInterviews.set(upcoming);
    this.pastInterviews.set(past);

  } catch (error) {

    console.error(
      'ERROR: Failed to retrieve interviews.',
      error
    );

    this.toast.error(
      'Unable to retrieve interview data. Please try again.'
    );
  }
}

    private getInterviewDateTime(interview: any): Date {

    const date = new Date(interview.interviewDate);

    const [hours, minutes] =
      interview.interviewTime.split(':');

    date.setHours(
      Number(hours),
      Number(minutes),
      0,
      0
    );

    return date;
  }

  

  async applyDecision(): Promise<void> {

  const interview = this.selectedInterview();
  const decision = this.selectedDecision();

  if (!interview || !decision) {
    return;
  }

  try {

    await this.interviewService.makeFinalDecision(
      interview.id,
      decision
    );

    // Update UI
    interview.applicationStatus = decision;

    this.pastInterviews.set([
      ...this.pastInterviews()
    ]);

    // Close popup
    this.showDecisionPopup.set(false);
    this.selectedInterview.set(null);
    this.selectedDecision.set(null);

    this.toast.success(
      decision === 'approved'
        ? 'Application approved successfully.'
        : 'Application rejected successfully.'
    );

  } catch (error) {

    console.error(
      'Failed to update application status:',
      error
    );

    this.toast.error(
      'Could not update application status.'
    );
  }
}

async saveNotes(interview: any): Promise<void> {
  try {
    await this.interviewService.updateInterviewNotes(
      interview.id,
      interview.notes
    );

    this.toast.success('Interview notes saved successfully.');

  } catch (error) {
    console.error('Failed to save interview notes:', error);

    this.toast.error('Could not save interview notes.');
  }
}


  scheduleInterview(applicationId: number): void {

    this.router.navigate([
      '/officer/interviews',
      applicationId
    ]);

  }

}