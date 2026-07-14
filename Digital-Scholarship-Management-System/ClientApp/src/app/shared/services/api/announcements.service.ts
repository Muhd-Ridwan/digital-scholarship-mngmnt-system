import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Announcement } from '../../models/announcement.model';

/**
 * Announcements API (AD2 / FR-31, FR-35). MOCK for now.
 * Phase 1: swap `of(...)` for `this.http.get<Announcement[]>(`${environment.apiUrl}/announcements`)`.
 */
@Injectable({ providedIn: 'root' })
export class AnnouncementsService {
  getAll(): Observable<Announcement[]> {
    return of([
      {
        id: 'a-1',
        title: 'Scheduled maintenance this weekend',
        body: 'The platform will be briefly unavailable on Saturday 02:00–03:00 for maintenance.',
        audience: 'All',
        status: 'Published',
        publishedAt: '10 Jul 2026',
      },
      {
        id: 'a-2',
        title: 'New sponsor onboarding guidelines',
        body: 'Sponsors must upload an updated SSM certificate before posting new listings.',
        audience: 'Sponsor',
        status: 'Draft',
        publishedAt: null,
      },
    ]);
  }
}
