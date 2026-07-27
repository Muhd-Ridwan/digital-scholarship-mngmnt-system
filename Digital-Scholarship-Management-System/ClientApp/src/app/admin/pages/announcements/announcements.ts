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

  protected readonly visible = computed(() => {
    const filter = this.statusFilter();
    const list = this.announcements();
    return filter === 'All' ? list : list.filter((a) => a.status === filter);
  });

  constructor() {
    // MOCK read now; becomes a real HTTP GET once the backend endpoint is available.
    this.announcementsApi.getAll().subscribe((list) => this.announcements.set(list));
  }

  protected audienceLabel(audience: AnnouncementAudience): string {
    return audience === 'All' ? 'All roles' : audience;
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

  private stampToday(): string {
    return new Date().toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  // Composer — publish immediately (stamps publishedAt).
  private publishNew(title: string, body: string, audience: AnnouncementAudience): boolean {
    const t = title.trim();
    const b = body.trim();
    if (!t || !b) {
      this.toastService.error('Title and message are both required to publish');
      return false;
    }
    this.announcements.update((list) => [
      { id: `a-${Date.now()}`, title: t, body: b, audience, status: 'Published', publishedAt: this.stampToday() },
      ...list,
    ]);
    this.toastService.success(`Announcement published to ${this.audienceLabel(audience)}`);
    return true;
  }

  // Composer — save as draft (no publishedAt stamp).
  private saveDraft(title: string, body: string, audience: AnnouncementAudience): boolean {
    const t = title.trim();
    if (!t) {
      this.toastService.error('A title is required to save a draft');
      return false;
    }
    this.announcements.update((list) => [
      { id: `a-${Date.now()}`, title: t, body: body.trim(), audience, status: 'Draft', publishedAt: null },
      ...list,
    ]);
    this.toastService.success('Draft saved');
    return true;
  }

  protected onCompose(
    mode: 'publish' | 'draft',
    titleInput: HTMLInputElement,
    bodyInput: HTMLTextAreaElement,
    audienceSelect: HTMLSelectElement,
  ): void {
    const audience = audienceSelect.value as AnnouncementAudience;
    const ok =
      mode === 'publish'
        ? this.publishNew(titleInput.value, bodyInput.value, audience)
        : this.saveDraft(titleInput.value, bodyInput.value, audience);
    if (ok) {
      titleInput.value = '';
      bodyInput.value = '';
      audienceSelect.value = 'All';
    }
  }

  // Row action — publish an existing draft (stamps publishedAt now).
  protected publishDraft(announcement: Announcement): void {
    if (announcement.status !== 'Draft') return;
    this.announcements.update((list) =>
      list.map((a) =>
        a.id === announcement.id ? { ...a, status: 'Published', publishedAt: this.stampToday() } : a,
      ),
    );
    this.toastService.success(`"${announcement.title}" published`);
  }

  // Row action — archive a published announcement. KEEPS publishedAt as evidence.
  protected archive(announcement: Announcement): void {
    if (announcement.status !== 'Published') return;
    this.announcements.update((list) =>
      list.map((a) => (a.id === announcement.id ? { ...a, status: 'Archived' } : a)),
    );
    this.toastService.success(`"${announcement.title}" archived`);
  }

  // Row action — hard delete, allowed ONLY on a draft.
  protected deleteDraft(announcement: Announcement): void {
    if (announcement.status !== 'Draft') return;
    this.announcements.update((list) => list.filter((a) => a.id !== announcement.id));
    this.toastService.success(`Draft "${announcement.title}" deleted`);
  }
}
