export interface SponsorReportTotals {
    scholarships: number;
    openListings: number;
    closedListings: number;
    totalApplicants: number;
    totalDisbursed: number;
}

export interface SponsorReportScholarship {
    id: number;
    title: string;
    fundType: string;
    deadline: string;
    status: 'draft' | 'open' | 'closed';
    applications: number;
    disbursed: number;
}

export interface SponsorDisbursementRecord {
    id: number;
    scholarshipTitle: string;
    studentName: string;
    disbursedAmount: number;
    disbursedAt: string;
}

export interface SponsorReportSummary {
    totals: SponsorReportTotals;
    scholarships: SponsorReportScholarship[];
    disbursements: SponsorDisbursementRecord[];
}