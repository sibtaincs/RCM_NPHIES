namespace NPhies_FHIR_Integration.ApiService.Security;

/// <summary>
/// The canonical catalogue of granular permissions, in "category:action" form.
/// These are seeded into the AppPermissions table and can be granted to roles
/// from the admin UI. Code should reference these constants when guarding
/// endpoints with <c>[HasPermission(...)]</c>.
/// </summary>
public static class Permissions
{
    // ---- Claims ----
 public const string ClaimsRead = "claims:read";
    public const string ClaimsCreate = "claims:create";
public const string ClaimsUpdate = "claims:update";
    public const string ClaimsDelete = "claims:delete";
    public const string ClaimsSubmit = "claims:submit";
    public const string ClaimsTechnicalReview = "claims:technical-review";
    public const string ClaimsMedicalReview = "claims:medical-review";

    // ---- Pre-authorization ----
    public const string PreAuthRead = "preauth:read";
    public const string PreAuthCreate = "preauth:create";
    public const string PreAuthSubmit = "preauth:submit";
    public const string PreAuthApprove = "preauth:approve";

    // ---- Eligibility ----
  public const string EligibilityRead = "eligibility:read";
    public const string EligibilityCheck = "eligibility:check";

    // ---- Patients ----
    public const string PatientsRead = "patients:read";
    public const string PatientsCreate = "patients:create";
    public const string PatientsUpdate = "patients:update";

    // ---- Master data ----
    public const string MasterDataRead = "masterdata:read";
    public const string MasterDataManage = "masterdata:manage";

// ---- Payments ----
    public const string PaymentsRead = "payments:read";
    public const string PaymentsManage = "payments:manage";

  // ---- Reports / dashboards ----
    public const string ReportsRead = "reports:read";
    public const string ReportsExport = "reports:export";

    // ---- Access control ----
    public const string UsersRead = "users:read";
    public const string UsersManage = "users:manage";
    public const string RolesRead = "roles:read";
    public const string RolesManage = "roles:manage";

  // ---- Audit ----
    public const string AuditRead = "audit:read";

    /// <summary>Full catalogue with metadata used for seeding.</summary>
  public static readonly (string Name, string Category, string Action, string Description)[] Catalogue =
    [
   (ClaimsRead, "Claims", "read", "View claims"),
        (ClaimsCreate, "Claims", "create", "Create claims"),
        (ClaimsUpdate, "Claims", "update", "Edit claims"),
        (ClaimsDelete, "Claims", "delete", "Delete claims"),
        (ClaimsSubmit, "Claims", "submit", "Submit claims to NPHIES"),
   (ClaimsTechnicalReview, "Claims", "technical-review", "Perform technical review of claims"),
     (ClaimsMedicalReview, "Claims", "medical-review", "Perform medical review of claims"),

    (PreAuthRead, "PreAuthorization", "read", "View pre-authorizations"),
        (PreAuthCreate, "PreAuthorization", "create", "Create pre-authorizations"),
  (PreAuthSubmit, "PreAuthorization", "submit", "Submit pre-authorizations to NPHIES"),
  (PreAuthApprove, "PreAuthorization", "approve", "Approve or reject pre-authorizations"),

     (EligibilityRead, "Eligibility", "read", "View eligibility results"),
     (EligibilityCheck, "Eligibility", "check", "Run eligibility checks"),

        (PatientsRead, "Patients", "read", "View patients"),
        (PatientsCreate, "Patients", "create", "Register patients"),
        (PatientsUpdate, "Patients", "update", "Edit patient details"),

        (MasterDataRead, "MasterData", "read", "View master data"),
        (MasterDataManage, "MasterData", "manage", "Create, edit and delete master data"),

        (PaymentsRead, "Payments", "read", "View payments and reconciliation"),
        (PaymentsManage, "Payments", "manage", "Manage payments and reconciliation"),

        (ReportsRead, "Reports", "read", "View dashboards and reports"),
   (ReportsExport, "Reports", "export", "Export reports"),

        (UsersRead, "AccessControl", "read", "View users"),
        (UsersManage, "AccessControl", "manage", "Create, edit and deactivate users"),
        (RolesRead, "AccessControl", "read", "View roles and permissions"),
        (RolesManage, "AccessControl", "manage", "Create roles and grant permissions"),

        (AuditRead, "Audit", "read", "View audit logs")
    ];
}
