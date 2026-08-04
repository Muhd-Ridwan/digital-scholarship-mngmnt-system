export type ApplicationStatus = 'pending' | 'under_review' | 'approved' | 'rejected';

export interface Application {
  id: number;
  scholarshipId: number;
  scholarshipTitle: string;
  status: ApplicationStatus;
  submittedAt: string;
  decisionAt: string | null;
}
