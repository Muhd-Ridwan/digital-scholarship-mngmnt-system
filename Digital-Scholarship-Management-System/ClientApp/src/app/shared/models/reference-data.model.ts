// Distinct values in use across the Scholarships table. Read-only — these are derived
// from what sponsors entered, not a list anyone maintains.
// Category names match the Scholarship column names.

export type ReferenceCategory = 'FundType' | 'StudyLocation' | 'OrganisationType';

export interface ReferenceDataItem {
  category: ReferenceCategory;
  value: string;
}

// Display names for the categories — one place, so screens don't drift.
export const REFERENCE_CATEGORY_LABELS: Record<ReferenceCategory, string> = {
  FundType: 'Fund Type',
  StudyLocation: 'Study Location',
  OrganisationType: 'Organisation Type',
};
