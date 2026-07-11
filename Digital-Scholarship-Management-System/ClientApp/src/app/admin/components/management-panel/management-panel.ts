import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

export interface ManagementItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-management-panel',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './management-panel.html',
})
export class ManagementPanel {
  readonly items = input.required<ManagementItem[]>();
  readonly viewAllRoute = input<string | null>(null);
}
