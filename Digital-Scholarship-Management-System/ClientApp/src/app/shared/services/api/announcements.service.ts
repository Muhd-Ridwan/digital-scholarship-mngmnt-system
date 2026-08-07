import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../../environments/environment';
import {
  Announcement,
  AnnouncementAudience,
  AnnouncementStatus,
  FeedAnnouncement,
} from '../../models/announcement.model';

// Announcements, backed by DynamoDB. The item key is (audience, sk), so updates and deletes
// send both — an id on its own cannot locate a row.
@Injectable({ providedIn: 'root' })
export class AnnouncementsService {
  private readonly http = inject(HttpClient);

  private async authHeader(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      throw new Error('Not authenticated.');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }

  // Admin view — every status, every audience.
  async getAll(): Promise<Announcement[]> {
    const headers = await this.authHeader();
    return firstValueFrom(
      this.http.get<Announcement[]>(`${environment.apiUrl}/announcements`, { headers }),
    );
  }

  // Published items for the caller's own role. The server derives the audience from the
  // token — there is no parameter to pass, and that is deliberate.
  async getFeed(): Promise<FeedAnnouncement[]> {
    const headers = await this.authHeader();
    return firstValueFrom(
      this.http.get<FeedAnnouncement[]>(`${environment.apiUrl}/announcements/feed`, { headers }),
    );
  }

  // One marker per (user, announcement). Repeating it overwrites the same key, so it is safe
  // to call again if a click is retried.
  async markRead(announcementId: string): Promise<void> {
    const headers = await this.authHeader();
    await firstValueFrom(
      this.http.post<void>(
        `${environment.apiUrl}/announcements/read`,
        { announcementId },
        { headers },
      ),
    );
  }

  async create(
    title: string,
    body: string,
    audience: AnnouncementAudience,
    status: AnnouncementStatus,
  ): Promise<Announcement> {
    const headers = await this.authHeader();
    return firstValueFrom(
      this.http.post<Announcement>(
        `${environment.apiUrl}/announcements`,
        { title, body, audience, status },
        { headers },
      ),
    );
  }

  async setStatus(item: Announcement, status: AnnouncementStatus): Promise<Announcement> {
    const headers = await this.authHeader();
    return firstValueFrom(
      this.http.put<Announcement>(
        `${environment.apiUrl}/announcements`,
        { audience: item.audience, sk: item.sk, status },
        { headers },
      ),
    );
  }

  // sk contains '#', which would terminate the URL — encode it or every delete 404s.
  async remove(item: Announcement): Promise<void> {
    const headers = await this.authHeader();
    const sk = encodeURIComponent(item.sk);
    await firstValueFrom(
      this.http.delete<void>(
        `${environment.apiUrl}/announcements?audience=${item.audience}&sk=${sk}`,
        { headers },
      ),
    );
  }
}
