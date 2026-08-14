export type AnnouncementAudience = 'All' | 'Student' | 'Officer' | 'Sponsor';

export type AnnouncementStatus = 'Draft' | 'Published' | 'Archived';

export interface Announcement {
  id: string;
  // DynamoDB sort key (createdAt#id). With audience it forms the item's full key — the id
  // alone cannot address a row, so both go back on every update and delete.
  sk: string;
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
