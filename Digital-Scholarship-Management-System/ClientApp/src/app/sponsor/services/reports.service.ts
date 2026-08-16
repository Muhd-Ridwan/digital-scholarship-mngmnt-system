import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';
import { SponsorReportSummary } from '../models/sponsor-report.model';

@Injectable({ providedIn: 'root' })
export class SponsorReportsService {
    private readonly http = inject(HttpClient);

    async getSummary(): Promise<SponsorReportSummary> {
        const session = await fetchAuthSession();
        const accessToken = session.tokens?.accessToken?.toString();
        if (!accessToken) {
            throw new Error('Not authenticated.');
        }
        return firstValueFrom(
            this.http.get<SponsorReportSummary>(`${environment.apiUrl}/reports/sponsor-summary`, {
                headers: { Authorization: `Bearer ${accessToken}` },
            }),
        );
    }
}