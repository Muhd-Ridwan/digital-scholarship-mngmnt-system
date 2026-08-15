import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../../../shared/services/toast.service';
import {
  Announcement,
  AnnouncementAudience,
  AnnouncementStatus,
} from '../../../shared/models/announcement.model';
import { AnnouncementsService } from '../../../shared/services/api/announcements.service';

type StatusFilter = 'All' | AnnouncementStatus;

@Component({
  selector: 'app-admin-announcements',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './announcements.html',
})
export class AdminAnnouncements {
  private readonly toastService = inject(ToastService);
  private readonly announcementsApi = inject(AnnouncementsService);

  protected readonly audiences: AnnouncementAudience[] = ['All', 'Student', 'Officer', 'Sponsor'];
  protected readonly filters: StatusFilter[] = ['All', 'Draft', 'Published', 'Archived'];

  protected readonly announcements = signal<Announcement[]>([]);
  protected readonly statusFilter = signal<StatusFilter>('All');
  // Every action is a round trip now — without this a double-click posts twice.
  protected readonly busy = signal(false);

  protected readonly visible = computed(() => {
    const filter = this.statusFilter();
    const list = this.announcements();
    return filter === 'All' ? list : list.filter((a) => a.status === filter);
  });

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      this.announcements.set(await this.announcementsApi.getAll());
    } catch {
      this.toastService.error('Could not load announcements');
    }
  }

  protected audienceLabel(audience: AnnouncementAudience): string {
    return audience === 'All' ? 'All roles' : audience;
  }

  // The API returns ISO timestamps; the row shows a readable date.
  protected formatDate(iso: string | null): string {
    if (!iso) return '';
    const date = new Date(iso);
    return Number.isNaN(date.getTime())
      ? ''
      : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  protected setFilter(filter: StatusFilter): void {
    this.statusFilter.set(filter);
  }

  protected badgeClass(status: AnnouncementStatus): string {
    switch (status) {
      case 'Published':
        return 'bg-status-success/10 text-status-success';
      case 'Archived':
        return 'bg-status-warning/10 text-status-warning';
      default:
        return 'bg-ink-800 text-mist-400';
    }
  }

  private replace(updated: Announcement): void {
    this.announcements.update((list) => list.map((a) => (a.id === updated.id ? updated : a)));
  }

  protected async onCompose(
    mode: 'publish' | 'draft',
    titleInput: HTMLInputElement,
    bodyInput: HTMLTextAreaElement,
    audienceSelect: HTMLSelectElement,
  ): Promise<void> {
    if (this.busy()) return;

    const title = titleInput.value.trim();
    const body = bodyInput.value.trim();
    const audience = audienceSelect.value as AnnouncementAudience;

    // The server requires both for either mode; check here so a draft doesn't round-trip to fail.
    if (!title || !body) {
      this.toastService.error('Title and message are both required');
      return;
    }

    const status: AnnouncementStatus = mode === 'publish' ? 'Published' : 'Draft';

    this.busy.set(true);
    try {
      const created = await this.announcementsApi.create(title, body, audience, status);
      this.announcements.update((list) => [created, ...list]);
      this.toastService.success(
        mode === 'publish'
          ? `Announcement published to ${this.audienceLabel(audience)}`
          : 'Draft saved',
      );
      titleInput.value = '';
      bodyInput.value = '';
      audienceSelect.value = 'All';
    } catch {
      this.toastService.error('Could not save the announcement');
    } finally {
      this.busy.set(false);
    }
  }

  // Row action — publish an existing draft. The server stamps publishedAt.
  protected async publishDraft(announcement: Announcement): Promise<void> {
    if (announcement.status !== 'Draft' || this.busy()) return;

    this.busy.set(true);
    try {
      this.replace(await this.announcementsApi.setStatus(announcement, 'Published'));
      this.toastService.success(`"${announcement.title}" published`);
    } catch {
      this.toastService.error('Could not publish the announcement');
    } finally {
      this.busy.set(false);
    }
  }

  // Row action — archive a published announcement. The server keeps publishedAt as evidence.
  protected async archive(announcement: Announcement): Promise<void> {
    if (announcement.status !== 'Published' || this.busy()) return;

    this.busy.set(true);
    try {
      this.replace(await this.announcementsApi.setStatus(announcement, 'Archived'));
      this.toastService.success(`"${announcement.title}" archived`);
    } catch {
      this.toastService.error('Could not archive the announcement');
    } finally {
      this.busy.set(false);
    }
  }

  // Row action — put an archived announcement back in the feed. The server clears its read
  // markers, so it returns as unread rather than silently invisible.
  protected async unarchive(announcement: Announcement): Promise<void> {
    if (announcement.status !== 'Archived' || this.busy()) return;

    this.busy.set(true);
    try {
      this.replace(await this.announcementsApi.setStatus(announcement, 'Published'));
      this.toastService.success(`"${announcement.title}" is live again`);
    } catch {
      this.toastService.error('Could not unarchive the announcement');
    } finally {
      this.busy.set(false);
    }
  }

  // Row action — hard delete, allowed ONLY on a draft. The server enforces it too.
  protected async deleteDraft(announcement: Announcement): Promise<void> {
    if (announcement.status !== 'Draft' || this.busy()) return;

    this.busy.set(true);
    try {
      await this.announcementsApi.remove(announcement);
      this.announcements.update((list) => list.filter((a) => a.id !== announcement.id));
      this.toastService.success(`Draft "${announcement.title}" deleted`);
    } catch {
      this.toastService.error('Could not delete the draft');
    } finally {
      this.busy.set(false);
    }
  }
}
