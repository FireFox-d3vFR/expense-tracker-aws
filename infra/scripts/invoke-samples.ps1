#!/usr/bin/env pwsh
# invoke-samples.ps1 - Authenticate with Cognito and smoke-test the live API
#
# Covers the full employee workflow:
#   GET  /me
#   POST /expenses          (create)
#   GET  /expenses          (list)
#   POST /expenses/{id}/submit
#   POST /expenses/{id}/receipt-url (upload)
#   GET  /finance/queue     (as finance manager)
#   POST /finance/expenses/{id}/review (approve)
#
# Usage:
#   ./invoke-samples.ps1 -ApiUrl https://xxx.execute-api.eu-west-1.amazonaws.com/dev `
#                        -UserPoolClientId <client-id> `
#                        -Region eu-west-1

param(
    [Parameter(Mandatory)][string]$ApiUrl,
    [Parameter(Mandatory)][string]$UserPoolClientId,
    [string]$Region        = "eu-west-1",
    [string]$EmployeeUser  = "employee1@example.test",
    [string]$EmployeePass  = "Employee1!",
    [string]$FinanceUser   = "finance1@example.test",
    [string]$FinancePass   = "Finance1!"
)

$ErrorActionPreference = "Stop"
$ApiUrl = $ApiUrl.TrimEnd('/')

function Get-CognitoToken {
    param([string]$ClientId, [string]$Username, [string]$Password, [string]$Region)

    $auth = aws cognito-idp initiate-auth `
        --client-id $ClientId `
        --auth-flow USER_PASSWORD_AUTH `
        --auth-parameters USERNAME=$Username,PASSWORD=$Password `
        --region $Region | ConvertFrom-Json

    return $auth.AuthenticationResult.IdToken
}

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Token, [hashtable]$Body = $null)

    $headers = @{ Authorization = "Bearer $Token"; "Content-Type" = "application/json" }
    $uri     = "$ApiUrl$Path"
    $json    = if ($Body) { $Body | ConvertTo-Json -Depth 5 } else { $null }

    Write-Host ""
    Write-Host "  $Method $Path" -ForegroundColor DarkCyan
    try {
        $response = if ($json) {
            Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -Body $json
        } else {
            Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers
        }
        $response | ConvertTo-Json -Depth 5
        return $response
    } catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            Write-Host "  Body: $($reader.ReadToEnd())" -ForegroundColor Red
        }
        return $null
    }
}

Write-Host ""
Write-Host "=== Expense Tracker — Live API Smoke Test ===" -ForegroundColor Cyan
Write-Host "  API URL : $ApiUrl"
Write-Host ""

# ── Authenticate ────────────────────────────────────────────────────────────
Write-Host "[Auth] Getting Employee token..." -ForegroundColor Yellow
$employeeToken = Get-CognitoToken -ClientId $UserPoolClientId -Username $EmployeeUser -Password $EmployeePass -Region $Region
Write-Host "  OK"

Write-Host "[Auth] Getting Finance Manager token..." -ForegroundColor Yellow
$financeToken = Get-CognitoToken -ClientId $UserPoolClientId -Username $FinanceUser -Password $FinancePass -Region $Region
Write-Host "  OK"

# ── Employee flow ────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "── Employee flow ──────────────────────────────────────────" -ForegroundColor Yellow

Write-Host "1. GET /me"
Invoke-Api -Method GET -Path "/me" -Token $employeeToken | Out-Null

Write-Host "2. POST /expenses (create)"
$expense = Invoke-Api -Method POST -Path "/expenses" -Token $employeeToken -Body @{
    amount      = 85.50
    currency    = "EUR"
    category    = "Travel"
    description = "Train ticket - Client visit"
    expenseDate = (Get-Date -Format "yyyy-MM-dd")
}

if (-not $expense) { Write-Host "Cannot continue without expense. Exiting." -ForegroundColor Red; exit 1 }
$expenseId = $expense.expenseId
Write-Host "  Created expense: $expenseId"

Write-Host "3. GET /expenses (list)"
Invoke-Api -Method GET -Path "/expenses" -Token $employeeToken | Out-Null

Write-Host "4. POST /expenses/$expenseId/receipt-url (upload)"
Invoke-Api -Method POST -Path "/expenses/$expenseId/receipt-url" -Token $employeeToken -Body @{
    operation   = "upload"
    fileName    = "receipt.jpg"
    contentType = "image/jpeg"
} | Out-Null

Write-Host "5. POST /expenses/$expenseId/submit"
Invoke-Api -Method POST -Path "/expenses/$expenseId/submit" -Token $employeeToken | Out-Null

# ── Finance flow ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "── Finance Manager flow ────────────────────────────────────" -ForegroundColor Yellow

Write-Host "6. GET /finance/queue"
Invoke-Api -Method GET -Path "/finance/queue" -Token $financeToken | Out-Null

Write-Host "7. POST /finance/expenses/$expenseId/review (approve)"
Invoke-Api -Method POST -Path "/finance/expenses/$expenseId/review" -Token $financeToken -Body @{
    decision = "approve"
} | Out-Null

Write-Host "8. GET /expenses/$expenseId (verify Approved)"
$final = Invoke-Api -Method GET -Path "/expenses/$expenseId" -Token $employeeToken
if ($final -and $final.status -eq "Approved") {
    Write-Host ""
    Write-Host "All checks passed — expense is Approved." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "WARNING: final status is not Approved." -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Smoke test complete ===" -ForegroundColor Cyan
