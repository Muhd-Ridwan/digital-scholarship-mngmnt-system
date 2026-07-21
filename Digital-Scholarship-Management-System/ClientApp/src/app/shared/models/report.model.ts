/**
 * Oversight report shapes (FR-33). Admin owns the report DTO, but the underlying numbers
 * aggregate cross-role data (Sponsor scholarships, Student applications, Officer awards),
 * so the values are a "pending teammates" blank — mock now, real aggregates in Phase 1.
 */

export type ReportTone = 'gold' | 'success' | 'warning' | 'danger';

export interface ReportStat {
  label: string;
  value: string;
  icon: string;
  tone: ReportTone;
  description: string;
}

export interface ScholarshipVolume {
  label: string;
  count: number;
  percent: number;
}
