import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InterviewService } from '../../services/interview.service';
import { ToastService } from '../../../shared/services/toast.service';
import { CommonModule } from '@angular/common';
import { AppIcons } from '../../../shared/icons';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-schedule-interview',
  standalone: true,
  imports: [
    FormsModule,
    CommonModule,
    RouterLink,
    LucideAngularModule

  ],
  templateUrl: './schedule-interview.html'
})
export class ScheduleInterviewComponent {

  private readonly interviewService = inject(InterviewService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly AppIcons = AppIcons;

  applicationId!: number;

  interviewDate = '';
  interviewTime = '';
  interviewMethod = 'Online';
  location = '';
  meetingLink = '';
  notes = '';

  constructor() {
    this.applicationId = Number(
      this.route.snapshot.paramMap.get('applicationId')
    );
  }

  async submit() {
    try {
      await this.interviewService.scheduleInterview({
        applicationId: this.applicationId,
        interviewDate: this.interviewDate,
        interviewTime: this.interviewTime,
        interviewMethod: this.interviewMethod,
        location: this.location,
        meetingLink: this.meetingLink,
        notes: this.notes
      });

      this.toast.success('Interview scheduled successfully.');

      this.router.navigate(['/officer/applications']);

    } catch (error) {
      console.error("Schedule interview error:", error);
      this.toast.error('Could not schedule interview.');
    }
  }
}