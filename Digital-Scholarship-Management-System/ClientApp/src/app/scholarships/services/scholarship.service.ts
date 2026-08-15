import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';
import { CreateScholarshipRequest, Scholarship, ScholarshipDetail, ScholarshipStatus, ScholarshipSummary } from '../models/scholarship.model';

@Injectable({ providedIn: 'root' })
  export class ScholarshipService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/scholarships`;

    private async authHeaders(): Promise<{ Authorization: string }> {
        const session = await fetchAuthSession();
        const accessToken = session.tokens?.accessToken?.toString();
        if (!accessToken) {
            throw new Error('Not authenticated.');
        }
        return { Authorization: `Bearer ${accessToken}` };
    }

    async getMine(): Promise<Scholarship[]> {
        const headers = await this.authHeaders();
        return firstValueFrom(
            this.http.get<Scholarship[]>(`${this.baseUrl}/mine`, { headers }),
        );
    }

    async create(request: CreateScholarshipRequest): Promise<{ id: number; message: string }> {
        const headers = await this.authHeaders();
        return firstValueFrom(
            this.http.post<{ id: number; message: string }>(this.baseUrl, request, { headers }),
        );
    }

    async publish( id: number ): Promise<{ id: number; status: ScholarshipStatus }> {
        const headers = await this.authHeaders();
        return firstValueFrom(
            this.http.post<{ id: number; status: ScholarshipStatus }>(`${this.baseUrl}/${id}/publish`,{},{ headers }),
        );
    }

    async close( id: number ): Promise<{ id: number; status: ScholarshipStatus }> {
        const headers = await this.authHeaders();
        return firstValueFrom(
            this.http.post<{ id: number; status: ScholarshipStatus }>(`${this.baseUrl}/${id}/close`,{},{ headers }),
        );
    }

    async delete( id: number ): Promise<{ message: string }> {
        const headers = await this.authHeaders();
        return firstValueFrom(
            this.http.delete<{ message: string }>(`${this.baseUrl}/${id}`, { headers })
        );
    }

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