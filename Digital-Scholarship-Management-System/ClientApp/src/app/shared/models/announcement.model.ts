/**
 * Platform announcements — Admin writes them (AD2); every role reads them (FR-35),
 * so the contract lives in `shared/`.
 *
 * Mirrors the `announcements` table (title / body / audience / status / published_at).
 */

export type AnnouncementAudience = 'All' | 'Student' | 'Officer' | 'Sponsor';

export type AnnouncementStatus = 'Draft' | 'Published';

export interface Announcement {
  id: string;
  title: string;
  body: string;
  audience: AnnouncementAudience;
  status: AnnouncementStatus;
  /** Display date string, or null while still a draft. */
  publishedAt: string | null;
}
