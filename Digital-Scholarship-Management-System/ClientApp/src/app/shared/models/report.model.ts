export type ReportTone = 'gold' | 'success' | 'warning' | 'danger';

export interface ReportStat {
  label: string;
  value: string;
  icon: string;
  tone: ReportTone;
  description: string;
}

// Applications grouped by status, for the pipeline panel.
export type ApplicationStatus = 'Submitted' | 'Under Review' | 'Approved' | 'Rejected';

export interface StatusVolume {
  status: ApplicationStatus;
  count: number;
  percent: number;
}

// One row per scholarship in the main reports table. Read-only — these numbers
// are owned elsewhere, this screen just totals them up.
export interface ScholarshipReportRow {
  scholarship: string;
  sponsor: string;
  applications: number; // received
  awards: number; // granted
  slotsFilled: number;
  slotsTotal: number; // slots the scholarship offers
  status: 'Open' | 'Closed';
}

// Counts for the eligibility screening panel.
export interface ScreeningSummary {
  passed: number;
  screenedOut: number;
}
