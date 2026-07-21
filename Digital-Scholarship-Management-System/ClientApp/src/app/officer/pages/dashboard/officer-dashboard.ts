import { Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { DashboardHeader } from '../../../shared/components/dashboard-header/dashboard-header';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ActionCard } from '../../../shared/components/action-card/action-card';
import { ReviewQueue, ReviewQueueItem } from '../../components/review-queue/review-queue';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-officer-dashboard',
  standalone: true,
  imports: [DashboardHeader, StatCard, ActionCard, ReviewQueue, LucideAngularModule],
  templateUrl: './officer-dashboard.html',
})
export class OfficerDashboard {
  private readonly auth = inject(AuthService);
  readonly profile = this.auth.profile;

  protected readonly reviewQueue: ReviewQueueItem[] = [
    {
      id: '1',
      applicantName: 'Aisyah Rahman',
      scholarshipName: 'Merit Excellence Award',
      submittedAt: '3 hours ago',
      priority: 'urgent',
    },
    {
      id: '2',
      applicantName: 'Daniel Wong',
      scholarshipName: 'STEM Futures Grant',
      submittedAt: '1 day ago',
      priority: 'normal',
    },
    {
      id: '3',
      applicantName: 'Nur Alia Yusof',
      scholarshipName: 'Community Leadership Fund',
      submittedAt: '2 days ago',
      priority: 'normal',
    },
  ];
}
