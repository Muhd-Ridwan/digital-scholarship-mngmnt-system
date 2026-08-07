import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../../environments/environment';
import { ReportsSummary } from '../../models/report.model';

// One call for every Reports tab and the dashboard count tiles.
@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);

  async getSummary(): Promise<ReportsSummary> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      throw new Error('Not authenticated.');
    }
    return firstValueFrom(
      this.http.get<ReportsSummary>(`${environment.apiUrl}/reports/summary`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      }),
    );
  }
}
