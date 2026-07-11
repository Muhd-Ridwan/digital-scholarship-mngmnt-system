import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

export interface ReviewQueueItem {
  id: string;
  applicantName: string;
  scholarshipName: string;
  submittedAt: string;
  priority: 'normal' | 'urgent';
}

@Component({
  selector: 'app-review-queue',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './review-queue.html',
})
export class ReviewQueue {
  readonly items = input<ReviewQueueItem[]>([]);
  readonly viewAllRoute = input<string | null>(null);
}
