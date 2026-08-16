export type SponsorDecision = 'Pending' | 'Approved' | 'Rejected';

export interface SponsorProfile {
  id: string;
  companyName: string;
  registeredAt: string;
  status: SponsorDecision;
  // Set only once a decision is made.
  decidedAt: string | null;
  decidedBy: string | null;
}
