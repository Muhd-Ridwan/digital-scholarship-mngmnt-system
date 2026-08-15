// Shared user shape — used by any screen that lists, filters, or badges users.
// UserRole must stay spelled exactly like the enum in the API's User.cs. If the
// two drift, role comparisons fail silently instead of erroring.

export type UserRole = 'user' | 'officer' | 'sponsor' | 'admin';

export type UserStatus = 'Active' | 'Locked';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: UserStatus;
}

// Badge colours per role — kept here so every screen renders them the same.
export const ROLE_BADGE_CLASSES: Record<UserRole, string> = {
  user: 'bg-gold-500/10 text-gold-500',
  officer: 'bg-status-warning/10 text-status-warning',
  sponsor: 'bg-ink-800 text-mist-100',
  admin: 'bg-ink-800 text-mist-100',
};
