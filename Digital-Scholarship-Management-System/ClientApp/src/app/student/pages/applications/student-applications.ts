import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ApplicationService } from '../../services/application.service';
import { Application, ApplicationStatus } from '../../models/application.model';
import { ToastService } from '../../../shared/services/toast.service';
import {
  STAGE_LABELS,
  STAGE_CLASSES,
} from '../../components/application-progress/application-progress';

@Component({
  selector: 'app-student-applications',
  standalone: true,
  imports: [DatePipe, RouterLink, LucideAngularModule],
  templateUrl: './student-applications.html',
})
export class StudentApplications {
  private readonly applicationService = inject(ApplicationService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(true);
  protected readonly applications = signal<Application[]>([]);

  constructor() {
    this.loadApplications();
  }

  private async loadApplications() {
    try {
      this.applications.set(await this.applicationService.getMyApplications());
    } catch {
      this.toast.error('Could not load your applications.');
    } finally {
      this.loading.set(false);
    }
  }

  protected statusLabel(status: ApplicationStatus): string {
    return STAGE_LABELS[status];
  }

  protected statusClasses(status: ApplicationStatus): string {
    return STAGE_CLASSES[status];
  }
}
