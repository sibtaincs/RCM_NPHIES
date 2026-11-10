Add-Type -AssemblyName System.Net.Http
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

$base = "https://localhost:7590"
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(60)

# ---- 1. Login ----
$body = '{"username":"admin","password":"Admin@12345!"}'
$content = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, "application/json")

try {
    $resp = $client.PostAsync("$base/api/auth/login", $content).Result
    Write-Host "LOGIN STATUS: $($resp.StatusCode)"
    $json = $resp.Content.ReadAsStringAsync().Result
} catch {
    Write-Host "LOGIN ERROR: $($_.Exception.InnerException.Message)"
    exit 1
}

if ($resp.StatusCode -ne "OK") {
    Write-Host "LOGIN BODY: $json"
    exit 1
}

$parsed = $json | ConvertFrom-Json
$token = $parsed.token.accessToken
if (-not $token) { Write-Host "NO TOKEN. BODY: $json"; exit 1 }
Write-Host "TOKEN OK (len=$($token.Length))"
Write-Host "ROLES: $($parsed.user.roles -join ',')"
Write-Host "PERMISSIONS COUNT: $($parsed.user.permissions.Count)"

# ---- 2. Call master data endpoints ----
$client.DefaultRequestHeaders.Authorization =
    New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

$endpoints = @(
 "/api/ServiceCodeMasters?skip=0&take=5",
    "/api/DiagnosisCodeMasters?skip=0&take=5",
    "/api/MedicationCodeMasters?skip=0&take=5",
    "/api/PayerMasters?skip=0&take=5",
  "/api/DoctorMasters?skip=0&take=5"
)

foreach ($ep in $endpoints) {
    try {
      $r = $client.GetAsync("$base$ep").Result
        $b = $r.Content.ReadAsStringAsync().Result
    $preview = $b.Substring(0, [Math]::Min(220, $b.Length))
        Write-Host ""
     Write-Host "GET $ep -> $($r.StatusCode)"
      Write-Host " $preview"
    } catch {
  Write-Host "GET $ep -> ERROR: $($_.Exception.InnerException.Message)"
    }
}
