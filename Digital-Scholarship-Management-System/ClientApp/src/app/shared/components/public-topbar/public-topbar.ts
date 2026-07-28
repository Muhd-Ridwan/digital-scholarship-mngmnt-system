import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-public-topbar',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './public-topbar.html',
})
export class PublicTopbar {
  private readonly themeService = inject(ThemeService);

  protected readonly theme = this.themeService.theme;
  protected readonly themeIcon = computed(() => (this.theme() === 'dark' ? 'sun' : 'moon'));

  toggleTheme() {
    this.themeService.toggle();
  }
}
