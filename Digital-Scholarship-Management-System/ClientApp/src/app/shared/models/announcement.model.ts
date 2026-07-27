export type AnnouncementAudience = 'All' | 'Student' | 'Officer' | 'Sponsor';

export type AnnouncementStatus = 'Draft' | 'Published' | 'Archived';

export interface Announcement {
  id: string;
  title: string;
  body: string;
  audience: AnnouncementAudience;
  status: AnnouncementStatus;
  // Display date. Stamped on first publish and kept after archiving, so we don't
  // lose the record of when a notice went out. Null only while it's a draft.
  publishedAt: string | null;
}
