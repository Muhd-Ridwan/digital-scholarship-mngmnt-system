import { Component, computed, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

export type StateTone = 'gold' | 'success' | 'warning' | 'danger';

const ICON_TONE_CLASSES: Record<StateTone, string> = {
  gold: 'bg-gold-500/10 text-gold-500',
  success: 'bg-status-success/10 text-status-success',
  warning: 'bg-status-warning/10 text-status-warning',
  danger: 'bg-status-danger/10 text-status-danger',
};

const PROGRESS_TONE_CLASSES: Record<StateTone, string> = {
  gold: 'bg-gold-500',
  success: 'bg-status-success',
  warning: 'bg-status-warning',
  danger: 'bg-status-danger',
};

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './stat-card.html',
})
export class StatCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly icon = input.required<string>();
  readonly tone = input<StateTone>('gold');
  readonly description = input<string | null>(null);
  readonly progress = input<number | null>(null);

  protected readonly iconToneClasses = computed(() => ICON_TONE_CLASSES[this.tone()]);
  protected readonly progressToneClasses = computed(() => PROGRESS_TONE_CLASSES[this.tone()]);
}
