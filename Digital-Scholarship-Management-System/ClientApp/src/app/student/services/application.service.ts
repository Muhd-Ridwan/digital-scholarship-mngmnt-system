import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';
import { Application } from '../models/application.model';

export interface CreateApplicationRequest {
  scholarshipId: number;
  existingDocumentIds: number[];
}

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private readonly http = inject(HttpClient);

  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();

    if (!accessToken) {
      throw new Error('Not authenticated');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }

  async getMyApplications(): Promise<Application[]> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.get<Application[]>(`${environment.apiUrl}/applications/mine`, { headers }),
    );
  }

  async apply(request: CreateApplicationRequest): Promise<{ id: number }> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.post<{ id: number }>(`${environment.apiUrl}/applications`, request, { headers }),
    );
  }
}
