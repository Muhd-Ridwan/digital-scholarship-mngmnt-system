import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OfficerApplicationService } from '../../services/officer-application.service';
import { RouterLink } from '@angular/router';
import { LucideAngularModule} from 'lucide-angular';

type ReportPeriod =
  | 'all'
  | 'week'
  | 'month'
  | 'year'
  | 'custom';

@Component({
  selector: 'app-officer-reports',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule],
  templateUrl: './officer-reports.html',
})

export class OfficerReports implements OnInit {

  private readonly applicationService =
    inject(OfficerApplicationService);

    protected readonly reportApplications =
    signal<any[]>([]);
  // ============================================================
  // REPORT PERIOD
  // ============================================================

  protected readonly selectedPeriod =
    signal<ReportPeriod>('all');

  protected readonly startDate =
    signal('');

  protected readonly endDate =
    signal('');


  // ============================================================
  // APPLICATION STATISTICS
  // ============================================================

  protected readonly totalApplications =
    signal(0);

  protected readonly pendingApplications =
    signal(0);

  protected readonly underReviewApplications =
    signal(0);

  protected readonly shortlistedApplications =
    signal(0);

  protected readonly approvedApplications =
    signal(0);

  protected readonly rejectedApplications =
    signal(0);


  // ============================================================
  // LOADING
  // ============================================================

  protected readonly loading =
    signal(true);


  // ============================================================
  // INITIALIZE
  // ============================================================

  async ngOnInit(): Promise<void> {

    await this.loadApplicationStatistics();

  }


  // ============================================================
  // LOAD APPLICATION STATISTICS
  // ============================================================

  async loadApplicationStatistics(): Promise<void> {

    try {

      this.loading.set(true);

      const applications =
        await this.applicationService.getAllApplications();

        this.reportApplications.set(applications);

    // filter
        const filteredApplications =
            applications.filter(application =>
            this.isWithinSelectedPeriod(
                application.submittedAt
            )
            );

      
      // Total

      this.totalApplications.set(
        filteredApplications.length
      );


      // Pending

      this.pendingApplications.set(
        filteredApplications.filter(
          application =>
            application.status === 'pending'
        ).length
      );


      // Under Review

      this.underReviewApplications.set(
        filteredApplications.filter(
          application =>
            application.status === 'under_review'
        ).length
      );


      // Shortlisted

      this.shortlistedApplications.set(
        filteredApplications.filter(
          application =>
            application.status === 'shortlisted'
        ).length
      );


      // Approved

      this.approvedApplications.set(
        filteredApplications.filter(
          application =>
            application.status === 'approved'
        ).length
      );


      // Rejected

      this.rejectedApplications.set(
        filteredApplications.filter(
          application =>
            application.status === 'rejected'
        ).length
      );


      console.log(
        'Filtered report data:',
        filteredApplications
      );


    } catch (error) {

      console.error(
        'Failed to load report statistics:',
        error
      );

    } finally {

      this.loading.set(false);

    }

  }

  //check date range
   private isWithinSelectedPeriod(
    submittedAt: string
  ): boolean {

    const submitted =
      new Date(submittedAt);

    const period =
      this.selectedPeriod();

      if (period === 'all') {
      return true;
    }

     if (period === 'custom') {

      if (!this.startDate() || !this.endDate()) {
        return true;
      }

      const start =
        new Date(this.startDate());

      const end =
        new Date(this.endDate());

      // Include the entire end date
      end.setHours(
        23,
        59,
        59,
        999
      );

      return submitted >= start &&
             submitted <= end;
    }

    const now = new Date();

     if (period === 'month') {

      const startOfMonth =
        new Date(
          now.getFullYear(),
          now.getMonth(),
          1
        );

      startOfMonth.setHours(
        0,
        0,
        0,
        0
      );

      return submitted >= startOfMonth;
    }

      if (period === 'year') {

      const startOfYear =
        new Date(
          now.getFullYear(),
          0,
          1
        );

      startOfYear.setHours(
        0,
        0,
        0,
        0
      );

      return submitted >= startOfYear;
    }


    return true;
  }

   async changePeriod(
    period: ReportPeriod
  ): Promise<void> {

    this.selectedPeriod.set(period);


    // Clear custom dates when changing
    // to a predefined period.

    if (period !== 'custom') {

      this.startDate.set('');
      this.endDate.set('');

    }


    await this.loadApplicationStatistics();

  }

   async changeStartDate(
    event: Event
  ): Promise<void> {

    const input =
      event.target as HTMLInputElement;

    this.startDate.set(
      input.value
    );


    // Automatically use custom mode

    this.selectedPeriod.set(
      'custom'
    );


    if (this.startDate() &&
        this.endDate()) {

      await this.loadApplicationStatistics();

    }

  }

   async changeEndDate(
    event: Event
  ): Promise<void> {

    const input =
      event.target as HTMLInputElement;

    this.endDate.set(
      input.value
    );


    // Automatically use custom mode

    this.selectedPeriod.set(
      'custom'
    );


    if (this.startDate() &&
        this.endDate()) {

      await this.loadApplicationStatistics();

    }

  }

  exportToCsv(): void {

  // Get the applications used for the report
  const applications = this.reportApplications();

  if (!applications || applications.length === 0) {
    console.warn('No application data available to export.');
    return;
  }

  // CSV columns
  const headers = [
    'Application ID',
    'Student Name',
    'Student Email',
    'Scholarship',
    'Status',
    'Submitted At',
    'Decision At'
  ];

  // Convert application data into CSV rows
  const rows = applications.map(application => [
    application.id,
    application.studentName,
    application.studentEmail,
    application.scholarshipTitle,
    application.status,
    application.submittedAt,
    application.decisionAt ?? ''
  ]);

  // Escape values so commas/quotes don't break the CSV
  const csvRows = [
    headers,
    ...rows
  ].map(row =>
    row.map(value => {
      const stringValue = String(value ?? '');

      return `"${stringValue.replace(/"/g, '""')}"`;
    }).join(',')
  );

  const csvContent =
    csvRows.join('\n');

  // Create downloadable file
  const blob = new Blob(
    [csvContent],
    {
      type: 'text/csv;charset=utf-8;'
    }
  );

  const url =
    window.URL.createObjectURL(blob);

  const link =
    document.createElement('a');

  link.href = url;

  const date =
    new Date()
      .toISOString()
      .split('T')[0];

  link.download =
    `scholarship-report-${date}.csv`;

  link.click();

  // Clean up
  window.URL.revokeObjectURL(url);
}
}

