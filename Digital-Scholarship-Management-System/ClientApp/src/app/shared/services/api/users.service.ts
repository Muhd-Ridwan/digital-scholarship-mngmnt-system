import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { User } from '../../models/user.model';
import { SponsorProfile } from '../../models/sponsor-profile.model';

/**
 * Users & access API (AD1 / FR-28, FR-29). MOCK for now.
 * Phase 1: swap `of(...)` for real endpoints, e.g.
 *   this.http.get<User[]>(`${environment.apiUrl}/users`)
 * Phase 2: register/lock/approve become POST/PATCH that hit the Cognito Admin API server-side.
 */
@Injectable({ providedIn: 'root' })
export class UsersService {
  getUsers(): Observable<User[]> {
    return of([
      { id: 'u-1', name: 'Ganesh Kumar', email: 'ganesh@student.apu.edu', role: 'Student', status: 'Active' },
      { id: 'u-2', name: 'Siti Lestari', email: 'siti@scholarhub.my', role: 'Officer', status: 'Active' },
      { id: 'u-3', name: 'TechCorp Sdn Bhd', email: 'ops@techcorp.my', role: 'Sponsor', status: 'Active' },
      { id: 'u-4', name: 'Old Sponsor Bhd', email: 'finance@oldsponsor.my', role: 'Sponsor', status: 'Locked' },
    ]);
  }

  // Pending sponsors use the SponsorProfile PLACEHOLDER — the Sponsor owner expands it later.
  getPendingSponsors(): Observable<SponsorProfile[]> {
    return of([{ id: 'sp-1', companyName: 'Green Future Sdn Bhd', ssmNumber: '202601099887' }]);
  }
}
