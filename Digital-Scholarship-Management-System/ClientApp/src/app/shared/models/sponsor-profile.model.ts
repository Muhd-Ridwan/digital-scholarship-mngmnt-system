/**
 * PLACEHOLDER — this type is owned by the Sponsor role.
 *
 * Admin references it for the sponsor-approval card (AD1 / FR-28). The Sponsor owner
 * expands the fields later (onboarding status, certificate S3 key, SSM validation
 * status, etc.). Admin imports the NAME only, so adding fields never breaks admin code.
 */
export interface SponsorProfile {
  id: string;
  companyName: string;
  ssmNumber: string;
  // Sponsor owner adds: onboardingStatus, certFileKey, ssmValidationStatus, ...
}
