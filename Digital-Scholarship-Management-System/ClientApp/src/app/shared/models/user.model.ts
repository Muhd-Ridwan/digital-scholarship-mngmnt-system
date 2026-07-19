/**
 * Shared user contract — imported by every role that displays or filters users
 * (admin users screen, admin audit log, and any future role screens).
 *
 * Keep `UserRole` identical to the backend `UserRole` enum in
 * `...API/Models/User.cs` (Student / Officer / SponsorProvider / Admin) so the
 * frontend and backend never drift on role names.
 */

export type UserRole = 'Student' | 'Officer' | 'Sponsor' | 'Admin';

export type UserStatus = 'Active' | 'Locked';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: UserStatus;
}

/** Tailwind badge classes per role — shared so every screen colours roles identically. */
export const ROLE_BADGE_CLASSES: Record<UserRole, string> = {
  Student: 'bg-gold-500/10 text-gold-500',
  Officer: 'bg-status-warning/10 text-status-warning',
  Sponsor: 'bg-ink-800 text-mist-100',
  Admin: 'bg-ink-800 text-mist-100',
};
