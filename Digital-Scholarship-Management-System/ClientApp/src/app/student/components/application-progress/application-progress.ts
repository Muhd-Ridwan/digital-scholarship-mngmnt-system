import { Component, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

export type ApplicationStage = 'submitted' | 'in_review' | 'approved' | 'rejected';

export interface ApplicationStatusItem {
  id: string;
  scholarshipName: string;
  stage: ApplicationStage;
  updatedAt: string;
}

const STAGE_LABELS: Record<ApplicationStage, string> = {
  submitted: 'Submitted',
  in_review: 'In Review',
  approved: 'Approved',
  rejected: 'Rejected',
};

const STAGE_CLASSES: Record<ApplicationStage, string> = {
  submitted: 'bg-mist-400/10 text-mist-400',
  in_review: 'bg-status-warning/10 text-status-warning',
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

  protected stageLabel(stage: ApplicationStage): string {
    return STAGE_LABELS[stage];
  }

  protected stageClasses(stage: ApplicationStage): string {
    return STAGE_CLASSES[stage];
  }
}
