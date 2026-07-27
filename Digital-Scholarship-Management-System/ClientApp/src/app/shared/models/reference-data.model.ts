// Lookup lists (universities, courses, study levels, categories) that feed the
// dropdowns and eligibility rules. Only the admin screens edit these; everywhere
// else just reads them, which is why the type lives in shared/.
// Matches the reference_data table.

export type ReferenceCategory = 'University' | 'Course' | 'StudyLevel' | 'Category';

export interface ReferenceDataItem {
  id: string;
  category: ReferenceCategory;
  code: string;
  label: string;
  isActive: boolean;
}

// Display names for the categories — one place, so screens don't drift.
export const REFERENCE_CATEGORY_LABELS: Record<ReferenceCategory, string> = {
  University: 'Universities / IPTS',
  Course: 'Courses',
  StudyLevel: 'Study Levels',
  Category: 'Categories',
};
