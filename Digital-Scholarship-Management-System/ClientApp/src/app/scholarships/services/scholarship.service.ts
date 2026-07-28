import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ScholarshipDetail, ScholarshipSummary } from '../models/scholarship.model';

@Injectable({ providedIn: 'root' })
export class ScholarshipService {
  private readonly http = inject(HttpClient);

  async getAll(): Promise<ScholarshipSummary[]> {
    return firstValueFrom(
      this.http.get<ScholarshipSummary[]>(`${environment.apiUrl}/scholarships`),
    );
  }

  async getById(id: number): Promise<ScholarshipDetail> {
    return firstValueFrom(
      this.http.get<ScholarshipDetail>(`${environment.apiUrl}/scholarships/${id}`),
    );
  }
}
