import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import {
  ReportStat,
  StatusVolume,
  ScholarshipReportRow,
  ScreeningSummary,
} from '../../models/report.model';

// Report totals pulled from across the system. Canned data for now — swap of(...)
// for this.http.get<ReportStat[]>(`${environment.apiUrl}/reports/stats`).

@Injectable({ providedIn: 'root' })
export class ReportsService {
  // Require user/officer/sponsor code/data for reference
  getStats(): Observable<ReportStat[]> {
    return of([
      { label: 'Total Listings', value: '24', icon: 'graduation-cap', tone: 'gold', description: 'Scholarships posted' },
      { label: 'Applications Received', value: '412', icon: 'file-text', tone: 'gold', description: 'Across all scholarships' },
      { label: 'Total Awarded', value: '86 · RM 1.1M', icon: 'hand-coins', tone: 'success', description: 'Count and value' },
      { label: 'Approval Rate', value: '31%', icon: 'circle-check', tone: 'success', description: 'Of decided applications' },
    ]);
  }

  // Require user/officer code/data for reference
  getApplicationsByStatus(): Observable<StatusVolume[]> {
    return of([
      { status: 'Submitted', count: 140, percent: 100 },
      { status: 'Under Review', count: 96, percent: 69 },
      { status: 'Approved', count: 86, percent: 61 },
      { status: 'Rejected', count: 90, percent: 64 },
    ]);
  }

  // Require user/officer/sponsor code/data for reference
  getByScholarship(): Observable<ScholarshipReportRow[]> {
    return of([
      { scholarship: 'STEM Innovation', sponsor: 'Yayasan Tech', applications: 118, awards: 20, slotsFilled: 20, slotsTotal: 25, status: 'Open' },
      { scholarship: 'Merit Excellence', sponsor: 'Maju Foundation', applications: 92, awards: 15, slotsFilled: 15, slotsTotal: 15, status: 'Closed' },
      { scholarship: 'SPM Diploma Bridging', sponsor: 'EduCare Bhd', applications: 70, awards: 12, slotsFilled: 12, slotsTotal: 20, status: 'Open' },
    ]);
  }

  // Require user/sponsor code/data for reference
  getScreeningSummary(): Observable<ScreeningSummary> {
    return of({ passed: 268, screenedOut: 144 });
  }
}
