import { Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import {
  ApplicationProgress,
  ApplicationStatusItem,
} from '../../components/application-progress/application-progress';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, ApplicationProgress, LucideAngularModule],
  templateUrl: './student-dashboard.html',
})
export class StudentDashboard {
  private readonly auth = inject(AuthService);

  readonly profile = this.auth.profile;

  protected readonly sampleApplications: ApplicationStatusItem[] = [
    {
      id: '1',
      scholarshipName: 'Merit Excellence Award',
      stage: 'in_review',
      updatedAt: '2 days ago',
    },
    { id: '2', scholarshipName: 'STEM Futures Grant', stage: 'approved', updatedAt: '1 week ago' },
    {
      id: '3',
      scholarshipName: 'Community Leadership Fund',
      stage: 'submitted',
      updatedAt: 'just now',
    },
  ];
}
