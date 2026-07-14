import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ReferenceDataItem } from '../../models/reference-data.model';

/**
 * Reference-data API (AD2 / FR-30). MOCK for now — returns canned data via `of(...)`.
 * Phase 1: inject HttpClient and swap each `of(...)` for a real call, e.g.
 *   return this.http.get<ReferenceDataItem[]>(`${environment.apiUrl}/reference-data`);
 */
@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  getAll(): Observable<ReferenceDataItem[]> {
    return of([
      { id: 'r-1', category: 'University', code: 'APU', label: 'Asia Pacific University (IPTS)', isActive: true },
      { id: 'r-2', category: 'University', code: 'UM', label: 'Universiti Malaya (IPTA)', isActive: true },
      { id: 'r-3', category: 'Course', code: 'CS', label: 'Computer Science', isActive: true },
      { id: 'r-4', category: 'StudyLevel', code: 'DEG', label: 'Degree', isActive: true },
      { id: 'r-5', category: 'StudyLevel', code: 'DIP', label: 'Diploma', isActive: false },
      { id: 'r-6', category: 'Category', code: 'STEM', label: 'STEM', isActive: true },
    ]);
  }
}
