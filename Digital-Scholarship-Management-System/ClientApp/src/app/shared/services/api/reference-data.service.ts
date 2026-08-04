import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../../environments/environment';
import { ReferenceDataItem } from '../../models/reference-data.model';

// Distinct FundType / StudyLocation / OrganisationType values from GET /api/reference-data.
@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly http = inject(HttpClient);

  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();

    if (!accessToken) {
      throw new Error('Not authenticated');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }

  async getAll(): Promise<ReferenceDataItem[]> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.get<ReferenceDataItem[]>(`${environment.apiUrl}/reference-data`, { headers }),
    );
  }
}
