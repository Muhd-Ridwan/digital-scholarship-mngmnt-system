export type AnnouncementAudience = 'All' | 'Student' | 'Officer' | 'Sponsor';

export type AnnouncementStatus = 'Draft' | 'Published' | 'Archived';

export interface Announcement {
  id: number;
  title: string;
  body: string;
  audience: AnnouncementAudience;
  status: AnnouncementStatus;
  // ISO timestamp. Stamped on first publish and kept after archiving, so we don't lose the
  // record of when a notice went out. Null only while it's a draft.
  publishedAt: string | null;
  createdAt: string;
  createdBy: string;
}

// The feed adds a per-caller read flag; the admin list has no such notion.
export interface FeedAnnouncement extends Announcement {
  read: boolean;
}
