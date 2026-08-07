// Placeholder. The sponsor side of the app owns this type and will fill in the
// rest of the fields later. The admin screens import the name only, so those
// additions can land without breaking anything here.

// Three states, not a boolean — "not approved" has to distinguish "not looked at yet"
// from "refused", or the decision record cannot be built.
export type SponsorDecision = 'Pending' | 'Approved' | 'Rejected';

export interface SponsorProfile {
  id: string;
  companyName: string;
  ssmNumber: string | null;
  registeredAt: string;
  status: SponsorDecision;
  // Set only once a decision is made.
  decidedAt: string | null;
  decidedBy: string | null;
  // still to come: certFileKey, ssmValidationStatus, ...
}
