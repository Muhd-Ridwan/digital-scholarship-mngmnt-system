import { Component, computed, effect, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../../auth/services/auth.service';
import { FeedAnnouncement } from '../../models/announcement.model';
import { AnnouncementsService } from '../../services/api/announcements.service';

// Announcement bell. Mounted once inside DashboardShell's navbar, so it loads once per app
// load rather than once per navigation — the reason the unread count is cheap.
@Component({
  selector: 'app-announcement-bell',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './announcement-bell.html',
})
export class AnnouncementBell {
  private readonly announcementsApi = inject(AnnouncementsService);
  private readonly auth = inject(AuthService);

  protected readonly items = signal<FeedAnnouncement[]>([]);
  protected readonly open = signal(false);
  protected readonly failed = signal(false);
  // Which row is expanded to full text. Bodies are clamped to 2 lines otherwise, or one long
  // notice fills the panel and the list stops being scannable.
  protected readonly expandedId = signal<number | null>(null);

  // Admin authors announcements and manages them at /admin/announcements — receiving your own
  // broadcast back as an unread badge is noise. Hidden rather than empty, and it skips the
  // feed call entirely so an admin session makes no announcement queries at all.
  // Undefined means the profile hasn't arrived yet: stay hidden, or the bell flashes for an
  // admin before the role is known.
  protected readonly visible = computed(() => {
    const role = this.auth.profile()?.role;
    return role !== undefined && role !== 'admin';
  });

  protected readonly unread = computed(() => this.items().filter((i) => !i.read).length);
  // Nobody acts differently on 14 vs 23, and the feed only fetches 10 anyway.
  protected readonly badge = computed(() => (this.unread() > 9 ? '9+' : String(this.unread())));

  private loadStarted = false;

  constructor() {
    // The profile resolves after construction, so the first load waits for it rather than
    // firing a feed request before we know whether this role should see the bell at all.
    effect(() => {
      if (this.visible() && !this.loadStarted) {
        this.loadStarted = true;
        void this.load();
      }
    });
  }

  private async load(): Promise<void> {
    try {
      this.items.set(await this.announcementsApi.getFeed());
      this.failed.set(false);
    } catch {
      // A failing feed must not break the navbar for every role — show it in the panel
      // instead of a toast on every page load.
      this.items.set([]);
      this.failed.set(true);
    }
  }

  protected toggle(): void {
    const next = !this.open();
    this.open.set(next);
    // Re-query on open so anything published mid-session shows up.
    if (next) void this.load();
  }

  protected close(): void {
    this.open.set(false);
  }

  // One click does both jobs: reveal the full text, and mark it read.
  protected async onSelect(item: FeedAnnouncement): Promise<void> {
    this.expandedId.update((id) => (id === item.id ? null : item.id));

    if (item.read) return;

    try {
      await this.announcementsApi.markRead(item.id);
      this.items.update((list) => list.map((i) => (i.id === item.id ? { ...i, read: true } : i)));
    } catch {
      // Leave it unread — the next open re-queries and corrects itself.
    }
  }

  protected formatDate(iso: string | null): string {
    if (!iso) return '';
    const date = new Date(iso);
    return Number.isNaN(date.getTime())
      ? ''
      : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }
}
