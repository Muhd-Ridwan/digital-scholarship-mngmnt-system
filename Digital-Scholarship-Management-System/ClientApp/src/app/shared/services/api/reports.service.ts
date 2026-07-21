import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ReportStat, ScholarshipVolume } from '../../models/report.model';

/**
 * Oversight reports API (FR-33). MOCK for now — the numbers aggregate cross-role data
 * that other owners provide, so these stay canned until Phase 1.
 * Phase 1: swap `of(...)` for real endpoints, e.g.
 *   this.http.get<ReportStat[]>(`${environment.apiUrl}/reports/stats`);
 */
@Injectable({ providedIn: 'root' })
export class ReportsService {
  getStats(): Observable<ReportStat[]> {
    return of([
      { label: 'Approval Rate', value: '31%', icon: 'circle-check', tone: 'success', description: 'Applications approved' },
      { label: 'Avg. Review Time', value: '2.4 days', icon: 'history', tone: 'gold', description: 'Submission to decision' },
      { label: 'Total Awarded', value: 'RM 1.1M', icon: 'hand-coins', tone: 'gold', description: 'Across all scholarships' },
      { label: 'Completion Rate', value: '78%', icon: 'target', tone: 'warning', description: 'Awards fully disbursed' },
    ]);
  }

  getApplicationsByScholarship(): Observable<ScholarshipVolume[]> {
    return of([
      { label: 'STEM Innovation', count: 118, percent: 82 },
      { label: 'Merit Excellence', count: 92, percent: 64 },
      { label: 'SPM Diploma Bridging', count: 70, percent: 48 },
    ]);
  }
}
