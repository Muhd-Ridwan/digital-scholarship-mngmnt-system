import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../../environments/environment';
import { AuditEntry } from '../../models/audit.model';
import { UserRole } from '../../models/user.model';

interface ApiAuditEntry {
  id: string;
  user: string;
  role: UserRole;
  timestamp: string;
  action: string;
}

export interface AuditQuery {
  from?: string;
  to?: string;
  role?: UserRole;
  person?: string;
}

// Activity log — GET /api/audit-log?from=&to=&role=&person=. Admin-only, reads DynamoDB.
@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);

  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();

    if (!accessToken) {
      throw new Error('Not authenticated');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }

  async getEntries(query: AuditQuery): Promise<AuditEntry[]> {
    const headers = await this.authHeaders();
    let params = new HttpParams();
    if (query.from) params = params.set('from', query.from);
    if (query.to) params = params.set('to', query.to);
    if (query.role) params = params.set('role', query.role);
    if (query.person) params = params.set('person', query.person);

    const apiEntries = await firstValueFrom(
      this.http.get<ApiAuditEntry[]>(`${environment.apiUrl}/audit-log`, { headers, params }),
    );
    return apiEntries.map((e) => ({
      id: e.id,
      user: e.user,
      role: e.role,
      action: e.action,
      timestamp: new Date(e.timestamp).toLocaleString('en-GB', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }),
    }));
  }
}
