import { Component, computed, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

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
  readonly navLinks = input<NavLink[]>([]);
  readonly fullName = input.required<string>();
  readonly email = input.required<string>();

  readonly logout = output<void>();

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

  closeMenu() {
    this.menuOpen.set(false);
  }

  onLogout() {
    this.closeMenu();
    this.logout.emit();
  }
}
