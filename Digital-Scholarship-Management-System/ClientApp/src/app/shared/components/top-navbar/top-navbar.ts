import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ThemeService } from '../../services/theme.service';

export interface NavLink {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-top-navbar',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './top-navbar.html',
})
export class TopNavbar {
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);

  // Links the brand/logo back to the current role's dashboard (/admin, /student, …),
  // derived from the first URL segment so it stays correct for every role.
  protected get homeRoute(): string {
    const segment = this.router.url.split(/[?#]/)[0].split('/').filter(Boolean)[0];
    return segment ? `/${segment}` : '/';
  }

  readonly navLinks = input<NavLink[]>([]);
  readonly fullName = input.required<string>();
  readonly email = input.required<string>();

  readonly logout = output<void>();

  protected readonly theme = this.themeService.theme;
  protected readonly themeIcon = computed(() => (this.theme() === 'dark' ? 'sun' : 'moon'));

  protected readonly menuOpen = signal(false);
  protected readonly initials = computed(() =>
    this.fullName()
      .trim()
      .split(/\s+/)
      .map((part) => part[0])
      .slice(0, 2)
      .join('')
      .toUpperCase(),
  );
  toggleMenu() {
    this.menuOpen.update((open) => !open);
  }

  toggleTheme() {
    this.themeService.toggle();
  }

  closeMenu() {
    this.menuOpen.set(false);
  }

  onLogout() {
    this.closeMenu();
    this.logout.emit();
  }
}
