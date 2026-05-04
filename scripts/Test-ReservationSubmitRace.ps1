param(
    [string]$Url = "http://localhost:5212/api/Reservation/submit",
    [string]$PayloadPath = ".\temp-submit.json",
    [int]$Count = 2,
    [int]$TimeoutSeconds = 60
)

if (-not (Test-Path -LiteralPath $PayloadPath)) {
    throw "Payload file not found: $PayloadPath"
}

$body = Get-Content -LiteralPath $PayloadPath -Raw
$jobs = @()

for ($i = 1; $i -le $Count; $i++) {
    $jobs += Start-Job -ArgumentList $Url, $body, $i, $TimeoutSeconds -ScriptBlock {
        param($requestUrl, $requestBody, $requestNumber, $requestTimeoutSeconds)

        $startedAt = Get-Date

        try {
            $response = Invoke-RestMethod `
                -Uri $requestUrl `
                -Method Post `
                -ContentType "application/json" `
                -Body $requestBody `
                -TimeoutSec $requestTimeoutSeconds

            [pscustomobject]@{
                RequestNumber = $requestNumber
                StartedAtUtc  = $startedAt.ToUniversalTime().ToString("o")
                FinishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
                Outcome       = "Succeeded"
                Response      = ($response | ConvertTo-Json -Depth 10 -Compress)
            }
        }
        catch {
            $responseBody = $null

            if ($_.Exception.Response) {
                try {
                    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                    $responseBody = $reader.ReadToEnd()
                    $reader.Dispose()
                }
                catch {
                    $responseBody = $null
                }
            }

            [pscustomobject]@{
                RequestNumber = $requestNumber
                StartedAtUtc  = $startedAt.ToUniversalTime().ToString("o")
                FinishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
                Outcome       = "Failed"
                ErrorMessage  = $_.Exception.Message
                Response      = $responseBody
            }
        }
    }
}

$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job -Force | Out-Null

$results |
    Sort-Object RequestNumber |
    Format-Table -AutoSize
