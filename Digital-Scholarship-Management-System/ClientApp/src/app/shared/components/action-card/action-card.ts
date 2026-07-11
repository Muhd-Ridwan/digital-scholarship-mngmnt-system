import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router'; // For app router management
import { LucideAngularModule } from 'lucide-angular';

export type ActionTone = 'gold' | 'success' | 'warning' | 'danger';

const ICON_TONE_CLASSES: Record<ActionTone, string> = {
  gold: 'bg-gold-500/10 text-gold-500',
  success: 'bg-status-success/10 text-status-success',
  warning: 'bg-status-warning/10 text-status-warning',
  danger: 'bg-status-danger/10 text-status-danger',
};

// Component is a decorator
@Component({
  selector: 'app-action-card',
  standalone: true, // this component manages its own dependencies directly (via imports)
  imports: [RouterLink, LucideAngularModule], //list of other components/directives/modules that this component's own template is allowed to use
  templateUrl: './action-card.html', // Custom html tag name
})
export class ActionCard {
  readonly label = input.required<string>();
  readonly description = input.required<string>();
  readonly icon = input.required<string>();
  readonly route = input.required<string>();
  readonly tone = input<ActionTone>('gold');

  protected readonly iconToneClasses = computed(() => ICON_TONE_CLASSES[this.tone()]);
}
