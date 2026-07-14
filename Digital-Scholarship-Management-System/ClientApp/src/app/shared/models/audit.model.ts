import { UserRole } from './user.model';

/**
 * One audit-log entry (AD3 / FR-32, FR-44). Admin owns the read/query shape; every role
 * writes entries at their action points, so the type lives in `shared/`.
 * Mirrors the DynamoDB audit item.
 */
export interface AuditEntry {
  id: string;
  timestamp: string;
  user: string;
  role: UserRole;
  action: string;
}
