import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { User } from '../../models/user.model';
import { SponsorProfile } from '../../models/sponsor-profile.model';

// Users and access. Canned data for now — swap of(...) for
// this.http.get<User[]>(`${environment.apiUrl}/users`) once the endpoints exist.
// Register / lock / approve become POST and PATCH calls, handled server-side.
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

  // SponsorProfile is still a placeholder — expect more fields on it later.
  getPendingSponsors(): Observable<SponsorProfile[]> {
    return of([{ id: 'sp-1', companyName: 'Green Future Sdn Bhd', ssmNumber: '202601099887' }]);
  }
}
