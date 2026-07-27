// Placeholder. The sponsor side of the app owns this type and will fill in the
// rest of the fields later. The admin screens import the name only, so those
// additions can land without breaking anything here.
export interface SponsorProfile {
  id: string;
  companyName: string;
  ssmNumber: string;
  // still to come: onboardingStatus, certFileKey, ssmValidationStatus, ...
}
