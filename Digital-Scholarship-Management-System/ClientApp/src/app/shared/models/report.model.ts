export type ReportTone = 'gold' | 'success' | 'warning' | 'danger';

export interface ReportStat {
  label: string;
  value: string;
  icon: string;
  tone: ReportTone;
  description: string;
}

// Applications grouped by status, for the pipeline panel.
export type ApplicationStatus = 'Pending' | 'Under Review' | 'Approved' | 'Rejected';

export interface StatusVolume {
  status: ApplicationStatus;
  count: number;
  percent: number;
}

// Scholarships tab. Withdrawn means the sponsor pulled it before the deadline.
// Pending: Withdrawn needs a RemovedAt column on Scholarship from the Sponsor side.
export type ScholarshipState = 'Open' | 'Closed' | 'Withdrawn';

export interface ScholarshipRow {
  id: string;
  title: string;
  sponsor: string;
  fundType: string;
  applications: number;
  deadline: string;
  status: ScholarshipState;
}

// Applications tab. Read-only — no action column, the Officer decides.
export interface ApplicationRow {
  id: string;
  student: string;
  scholarship: string;
  submitted: string;
  status: ApplicationStatus;
}

// Summary tab. Approved applications grouped by their scholarship's FundType.
export interface FundTypeAward {
  fundType: string;
  count: number;
}

// Values sponsors have used. Kept ungrouped so different spellings show up.
export interface ReferenceValue {
  category: 'FundType' | 'StudyLocation' | 'OrganisationType';
  value: string;
  count: number;
}

// Raw counts; labels and icons are added in the component.
export interface ReportTotals {
  scholarships: number;
  applications: number;
  pending: number;
  underReview: number;
  approved: number;
  rejected: number;
}

// GET /reports/summary — the whole screen in one response, aggregated in SQL.
export interface ReportsSummary {
  totals: ReportTotals;
  scholarships: ScholarshipRow[];
  applications: ApplicationRow[];
  awardsByFundType: FundTypeAward[];
  valuesInUse: ReferenceValue[];
}
