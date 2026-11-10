namespace NPhies_FHIR_Integration.ApiService.Security;

/// <summary>
/// Security-related constants
/// </summary>
public static class SecurityConstants
{
    // JWT Claims
    public const string USER_ID_CLAIM = "sub";
 public const string USERNAME_CLAIM = "preferred_username";
    public const string EMAIL_CLAIM = "email";
    public const string ROLE_CLAIM = "role";
    public const string ORGANIZATION_CLAIM = "organization";
 public const string DEPARTMENT_CLAIM = "department";
    public const string PERMISSION_CLAIM = "permissions";
    public const string MFA_CLAIM = "mfa_verified";

    // Roles
    public const string ADMIN_ROLE = "Admin";
    public const string RCM_PROCESSOR_ROLE = "RCMProcessor";
    public const string RCM_VIEWER_ROLE = "RCMViewer";
    public const string CLAIMS_PROCESSOR_ROLE = "ClaimsProcessor";
    public const string CLAIMS_REVIEWER_ROLE = "ClaimsReviewer";
    public const string TECHNICAL_REVIEWER_ROLE = "TechnicalReviewer";
    public const string MEDICAL_REVIEWER_ROLE = "MedicalReviewer";
    public const string PAYER_USER_ROLE = "PayerUser";
    public const string PROVIDER_USER_ROLE = "ProviderUser";
    public const string COMPLIANCE_OFFICER_ROLE = "ComplianceOfficer";
public const string AUDITOR_ROLE = "Auditor";
public const string ACCOUNTANT_ROLE = "Accountant";
public const string USER_ROLE = "User";

    // Permissions
    public const string READ_PERMISSION = "read";
    public const string CREATE_PERMISSION = "create";
    public const string UPDATE_PERMISSION = "update";
    public const string DELETE_PERMISSION = "delete";
  public const string APPROVE_PERMISSION = "approve";
    public const string REJECT_PERMISSION = "reject";
    public const string EXPORT_PERMISSION = "export";
    public const string ADMIN_PERMISSION = "admin";

    // FHIR Resources
    public const string READ_CLAIMS_PERMISSION = "read:claims";
    public const string CREATE_CLAIMS_PERMISSION = "create:claims";
  public const string UPDATE_CLAIMS_PERMISSION = "update:claims";
    public const string READ_ELIGIBILITY_PERMISSION = "read:eligibility";
    public const string READ_COMMUNICATIONS_PERMISSION = "read:communications";
  public const string WRITE_COMMUNICATIONS_PERMISSION = "write:communications";

    // Security Headers
    public const string SECURITY_HEADER_CSP = "Content-Security-Policy";
    public const string SECURITY_HEADER_HSTS = "Strict-Transport-Security";
    public const string SECURITY_HEADER_X_FRAME_OPTIONS = "X-Frame-Options";
    public const string SECURITY_HEADER_X_CONTENT_TYPE = "X-Content-Type-Options";
    public const string SECURITY_HEADER_REFERRER_POLICY = "Referrer-Policy";
    public const string SECURITY_HEADER_PERMISSIONS_POLICY = "Permissions-Policy";

    // Rate Limiting
    public const int DEFAULT_RATE_LIMIT = 100;
    public const int DEFAULT_RATE_LIMIT_WINDOW_SECONDS = 60;
    public const int STRICT_RATE_LIMIT = 10;
    public const int STRICT_RATE_LIMIT_WINDOW_SECONDS = 60;

    // Session
    public const int SESSION_TIMEOUT_MINUTES = 30;
    public const int ABSOLUTE_TIMEOUT_MINUTES = 480; // 8 hours
    public const int IDLE_TIMEOUT_MINUTES = 30;

    // Password Policy
    public const int MINIMUM_PASSWORD_LENGTH = 12;
    public const int MAXIMUM_PASSWORD_AGE_DAYS = 90;
    public const int PASSWORD_HISTORY_COUNT = 5;
    public const int ACCOUNT_LOCKOUT_THRESHOLD = 5;
    public const int ACCOUNT_LOCKOUT_DURATION_MINUTES = 15;

    // Data Protection
    public const string DATA_PROTECTION_PURPOSE = "NPhiesFhirIntegration";
    public const int ENCRYPTION_KEY_SIZE = 256;
    public const int HASH_ITERATIONS = 100000;

    // IP Whitelist/Blacklist
 public const string LOCALHOST = "127.0.0.1";
    public const string LOCALHOST_V6 = "::1";

    // Audit
    public const string AUDIT_EVENT_LOGIN = "LOGIN";
    public const string AUDIT_EVENT_LOGOUT = "LOGOUT";
    public const string AUDIT_EVENT_CREATE = "CREATE";
    public const string AUDIT_EVENT_UPDATE = "UPDATE";
    public const string AUDIT_EVENT_DELETE = "DELETE";
  public const string AUDIT_EVENT_EXPORT = "EXPORT";
    public const string AUDIT_EVENT_APPROVE = "APPROVE";
    public const string AUDIT_EVENT_REJECT = "REJECT";
    public const string AUDIT_EVENT_ACCESS_DENIED = "ACCESS_DENIED";
    public const string AUDIT_EVENT_SUSPICIOUS_ACTIVITY = "SUSPICIOUS_ACTIVITY";
}
