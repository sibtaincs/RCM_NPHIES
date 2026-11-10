namespace NPhies_FHIR_Integration.Common.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public const string ApiVersion = "v1";
    public const string ApiRoutePrefix = $"/api/{ApiVersion}";

    public static class Messages
    {
   public const string RecordNotFound = "Record not found";
 public const string RecordCreatedSuccessfully = "Record created successfully";
        public const string RecordUpdatedSuccessfully = "Record updated successfully";
        public const string RecordDeletedSuccessfully = "Record deleted successfully";
   public const string InternalServerError = "An internal server error occurred";
    }

    public static class FhirResourceTypes
    {
      public const string Patient = "Patient";
        public const string Provider = "Practitioner";
     public const string Claim = "Claim";
        public const string ClaimResponse = "ClaimResponse";
        public const string Organization = "Organization";
        public const string Coverage = "Coverage";
    }
}
