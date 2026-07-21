/**
 * Global reference/lookup data — Admin owns CRUD (AD2), but the Sponsor reads it
 * in the eligibility-rule builder, so the contract lives in `shared/`.
 *
 * Mirrors the `reference_data` table (category / code / label / is_active).
 */

export type ReferenceCategory = 'University' | 'Course' | 'StudyLevel' | 'Category';

export interface ReferenceDataItem {
  id: string;
  category: ReferenceCategory;
  code: string;
  label: string;
  isActive: boolean;
}

/** Human-readable heading per category, shared so Admin and Sponsor label them identically. */
export const REFERENCE_CATEGORY_LABELS: Record<ReferenceCategory, string> = {
  University: 'Universities / IPTS',
  Course: 'Courses',
  StudyLevel: 'Study Levels',
  Category: 'Categories',
};
