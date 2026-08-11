import { UserRole } from './user.model';

// One row in the activity log. Every part of the app writes these at the moment
// an action happens; the admin screen only reads them back.
export interface AuditEntry {
  id: string;
  timestamp: string;
  user: string;
  role: UserRole;
  action: string;
}
