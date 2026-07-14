import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../../../shared/services/toast.service';
import { Announcement, AnnouncementAudience } from '../../../shared/models/announcement.model';
import { AnnouncementsService } from '../../../shared/services/api/announcements.service';

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

  protected readonly announcements = signal<Announcement[]>([]);

  constructor() {
    // MOCK read now; becomes a real HTTP GET once the backend endpoint is available.
    this.announcementsApi.getAll().subscribe((list) => this.announcements.set(list));
  }

  protected audienceLabel(audience: AnnouncementAudience): string {
    return audience === 'All' ? 'All roles' : audience;
  }

  protected publish(title: string, body: string, audience: AnnouncementAudience): boolean {
    const t = title.trim();
    const b = body.trim();
    if (!t || !b) {
      this.toastService.error('Title and message are both required to publish');
      return false;
    }
    this.announcements.update((list) => [
      {
        id: `a-${Date.now()}`,
        title: t,
        body: b,
        audience,
        status: 'Published',
        publishedAt: new Date().toLocaleDateString('en-GB', {
          day: 'numeric',
          month: 'short',
          year: 'numeric',
        }),
      },
      ...list,
    ]);
    this.toastService.success(`Announcement published to ${this.audienceLabel(audience)}`);
    return true;
  }

  protected onPublishSubmit(
    titleInput: HTMLInputElement,
    bodyInput: HTMLTextAreaElement,
    audienceSelect: HTMLSelectElement,
  ): void {
    const published = this.publish(
      titleInput.value,
      bodyInput.value,
      audienceSelect.value as AnnouncementAudience,
    );
    if (published) {
      titleInput.value = '';
      bodyInput.value = '';
      audienceSelect.value = 'All';
    }
  }

  protected unpublish(announcement: Announcement): void {
    this.announcements.update((list) =>
      list.map((a) =>
        a.id === announcement.id ? { ...a, status: 'Draft', publishedAt: null } : a,
      ),
    );
    this.toastService.success(`"${announcement.title}" moved back to draft`);
  }
}
