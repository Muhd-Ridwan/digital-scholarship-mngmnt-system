import { Component, computed, input, output } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-dashboard-header',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './dashboard-header.html',
})
export class DashboardHeader {
  readonly fullName = input.required<string>();
  readonly subtitle = input.required<string>();
  readonly primaryActionLabel = input<string | null>(null);
  readonly primaryActionIcon = input<string | null>(null);
  readonly primaryAction = output<void>();

  protected readonly eyebrowDate = computed(() =>
    new Date()
      .toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })
      .toUpperCase(),
  );
}
