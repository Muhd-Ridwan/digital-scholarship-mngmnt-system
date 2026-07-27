import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { AuditEntry } from '../../models/audit.model';

// Activity log. Canned data for now — replace of(...) with
// this.http.get<AuditEntry[]>(`${environment.apiUrl}/audit-log`) once the endpoint exists.
@Injectable({ providedIn: 'root' })
export class AuditService {
  getEntries(): Observable<AuditEntry[]> {
    return of([
      { id: 'e-1', timestamp: '11 Jul 2026, 14:02', user: 'Siti Lestari', role: 'Officer', action: 'Approved application A-2041' },
      { id: 'e-2', timestamp: '11 Jul 2026, 13:40', user: 'Ganesh Kumar', role: 'Student', action: 'Submitted application A-2041' },
      { id: 'e-3', timestamp: '11 Jul 2026, 11:15', user: 'TechCorp Sdn Bhd', role: 'Sponsor', action: 'Published scholarship "Green Energy Fund"' },
      { id: 'e-4', timestamp: '10 Jul 2026, 09:30', user: 'Admin Rae', role: 'Admin', action: 'Locked account: Old Sponsor Bhd' },
      { id: 'e-5', timestamp: '10 Jul 2026, 08:12', user: 'Ganesh Kumar', role: 'Student', action: 'Logged in' },
      { id: 'e-6', timestamp: '9 Jul 2026, 19:47', user: 'Aisha Rahman', role: 'Student', action: 'Saved application as draft' },
      { id: 'e-7', timestamp: '9 Jul 2026, 16:05', user: 'Wei Jie Tan', role: 'Student', action: 'Edited application A-2035' },
      { id: 'e-8', timestamp: '8 Jul 2026, 10:30', user: 'Siti Lestari', role: 'Officer', action: 'Updated profile details' },
    ]);
  }
}
