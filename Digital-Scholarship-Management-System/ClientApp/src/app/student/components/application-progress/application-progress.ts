import { Component, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

export type ApplicationStage = 'pending' | 'under_review' | 'approved' | 'rejected';

export interface ApplicationStatusItem {
  id: string;
  scholarshipName: string;
  stage: ApplicationStage;
  updatedAt: string;
}

export const STAGE_LABELS: Record<ApplicationStage, string> = {
  pending: 'Submitted',
  under_review: 'In Review',
  approved: 'Approved',
  rejected: 'Rejected',
};

export const STAGE_CLASSES: Record<ApplicationStage, string> = {
  pending: 'bg-mist-400/10 text-mist-400',
  under_review: 'bg-status-warning/10 text-status-warning',
  approved: 'bg-status-success/10 text-status-success',
  rejected: 'bg-status-danger/10 text-status-danger',
};

@Component({
  selector: 'app-application-progress',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './application-progress.html',
})
export class ApplicationProgress {
  readonly applications = input<ApplicationStatusItem[]>([]);
  readonly loading = input(false);

  protected stageLabel(stage: ApplicationStage): string {
    return STAGE_LABELS[stage];
  }

  protected stageClasses(stage: ApplicationStage): string {
    return STAGE_CLASSES[stage];
  }
}
