import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  ApplicationProgress,
  ApplicationStatusItem,
} from '../../components/application-progress/application-progress';
import { AuthService } from '../../../auth/services/auth.service';
import { ApplicationService } from '../../services/application.service';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { StudentProfileService } from '../../services/student-profile.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, ApplicationProgress, LucideAngularModule],
  templateUrl: './student-dashboard.html',
})
export class StudentDashboard {
  private readonly auth = inject(AuthService);
  private readonly applicationService = inject(ApplicationService);
  private readonly scholarshipService = inject(ScholarshipService);
  private readonly profileService = inject(StudentProfileService);
  private readonly router = inject(Router);

  readonly profile = this.auth.profile;
  protected readonly applications = signal<ApplicationStatusItem[]>([]);
  protected readonly applicationsLoading = signal(true);
  protected readonly scholarshipsAvailableCount = signal(0);
  protected readonly profileCompletionPercent = signal(0);

  protected readonly activeApplicationsCount = computed(
    () =>
      this.applications().filter((a) => a.stage === 'pending' || a.stage === 'under_review').length,
  );

  protected readonly approvedCount = computed(
    () => this.applications().filter((a) => a.stage === 'approved').length,
  );

  constructor() {
    this.loadApplications();
    this.loadStats();
  }

  private async loadApplications() {
    try {
      const applications = await this.applicationService.getMyApplications();
      this.applications.set(
        applications.map((a) => ({
          id: String(a.id),
          scholarshipName: a.scholarshipTitle,
          stage: a.status,
          updatedAt: this.formatRelativeTime(a.decisionAt ?? a.submittedAt),
        })),
      );
    } finally {
      this.applicationsLoading.set(false);
    }
  }

  private async loadStats() {
    const [scholarships, profile] = await Promise.all([
      this.scholarshipService.getAll(),
      this.profileService.getProfile(),
    ]);
    const now = new Date();
    this.scholarshipsAvailableCount.set(
      scholarships.filter((s) => new Date(s.deadline) > now).length,
    );
    this.profileCompletionPercent.set(profile ? 100 : 0);
  }

  private formatRelativeTime(dateString: string): string {
    const diffMs = Date.now() - new Date(dateString).getTime();
    const diffMinutes = Math.round(diffMs / 60000);

    if (diffMinutes < 1) return 'just now';
    if (diffMinutes < 60) return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`;

    const diffHours = Math.round(diffMinutes / 60);
    if (diffHours < 24) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;

    const diffDays = Math.round(diffHours / 24);
    if (diffDays < 7) return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;

    const diffWeeks = Math.round(diffDays / 7);
    return `${diffWeeks} week${diffWeeks === 1 ? '' : 's'} ago`;
  }

  onViewApplications() {
    this.router.navigate(['/student/applications']);
  }
}
