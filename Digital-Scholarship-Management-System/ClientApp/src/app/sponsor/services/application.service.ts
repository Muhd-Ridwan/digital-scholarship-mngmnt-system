import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';
import { PendingDisbursement } from '../models/pending-disbursement.model';

@Injectable({ providedIn: 'root' })
export class ApplicationService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/applications`;

    private async authHeaders(): Promise<{ Authorization: string }> {
        const session = await fetchAuthSession();
        const accessToken = session.tokens?.accessToken?.toString();
        if (!accessToken) {
        throw new Error('Not authenticated.');
        }
        return { Authorization: `Bearer ${accessToken}` };
    }

    async getPendingDisbursements(): Promise<PendingDisbursement[]> {
        const headers = await this.authHeaders();
        return firstValueFrom(
        this.http.get<PendingDisbursement[]>(`${this.baseUrl}/sponsor/pending-disbursements`, { headers }),
        );
    }

    async disburse(id: number, disbursedAmount: number): Promise<{ id: number; message: string }> {
        const headers = await this.authHeaders();
        return firstValueFrom(
        this.http.post<{ id: number; message: string }>(
            `${this.baseUrl}/${id}/disburse`,
            { disbursedAmount },
            { headers },
        ),
        );
    }
}