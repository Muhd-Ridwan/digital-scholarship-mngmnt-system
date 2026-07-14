import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ROLE_BADGE_CLASSES, UserRole } from '../../../shared/models/user.model';
import { AuditEntry } from '../../../shared/models/audit.model';
import { AuditService } from '../../../shared/services/api/audit.service';

@Component({
  selector: 'app-admin-audit-log',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './audit-log.html',
})
export class AdminAuditLog {
  private readonly auditApi = inject(AuditService);

  protected readonly roles: ('All' | UserRole)[] = ['All', 'Student', 'Officer', 'Sponsor', 'Admin'];

  protected readonly roleFilter = signal<'All' | UserRole>('All');
  protected readonly personFilter = signal('');

  private readonly entries = signal<AuditEntry[]>([]);

  constructor() {
    // MOCK read now; becomes a real HTTP GET (DynamoDB-backed) in Phase 1/4.
    this.auditApi.getEntries().subscribe((list) => this.entries.set(list));
  }

  protected readonly filteredEntries = computed(() => {
    const role = this.roleFilter();
    const person = this.personFilter().trim().toLowerCase();
    return this.entries().filter((entry) => {
      const matchesRole = role === 'All' || entry.role === role;
      const matchesPerson = !person || entry.user.toLowerCase().includes(person);
      return matchesRole && matchesPerson;
    });
  });

  protected roleBadgeClasses(role: UserRole): string {
    return ROLE_BADGE_CLASSES[role];
  }

  protected onRoleFilterChange(value: string): void {
    this.roleFilter.set(value as 'All' | UserRole);
  }

  protected onPersonFilterChange(value: string): void {
    this.personFilter.set(value);
  }
}
