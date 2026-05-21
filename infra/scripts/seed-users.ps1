#!/usr/bin/env pwsh
# seed-users.ps1 - Create test users in Cognito and assign them to groups
#
# Usage:
#   ./seed-users.ps1 -UserPoolId us-east-1_XXXXXXXXX
#   ./seed-users.ps1 -UserPoolId us-east-1_XXXXXXXXX -Region eu-west-1

param(
    [Parameter(Mandatory)][string]$UserPoolId,
    [string]$Region          = "eu-west-1",
    [string]$EmployeeEmail   = "employee1@example.test",
    [string]$EmployeePass    = "Employee1!",
    [string]$FinanceEmail    = "finance1@example.test",
    [string]$FinancePass     = "Finance1!"
)

$ErrorActionPreference = "Stop"

function New-CognitoTestUser {
    param(
        [string]$UserPoolId,
        [string]$Username,
        [string]$Email,
        [string]$Password,
        [string]$Group,
        [string]$Region
    )

    Write-Host "  Creating '$Username' ($Email) in group '$Group'..." -ForegroundColor Cyan

    # Create the user with a forced-reset temp password, then set permanent password
    aws cognito-idp admin-create-user `
        --user-pool-id $UserPoolId `
        --username $Username `
        --user-attributes `
            Name=email,Value=$Email `
            Name=email_verified,Value=true `
        --temporary-password "Tmp1234!" `
        --region $Region `
        --output none 2>$null

    aws cognito-idp admin-set-user-password `
        --user-pool-id $UserPoolId `
        --username $Username `
        --password $Password `
        --permanent `
        --region $Region

    aws cognito-idp admin-add-user-to-group `
        --user-pool-id $UserPoolId `
        --username $Username `
        --group-name $Group `
        --region $Region

    Write-Host "    Done." -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Seeding Cognito test users ===" -ForegroundColor Cyan
Write-Host "  User Pool : $UserPoolId"
Write-Host "  Region    : $Region"
Write-Host ""

New-CognitoTestUser `
    -UserPoolId $UserPoolId `
    -Username   "employee1" `
    -Email      $EmployeeEmail `
    -Password   $EmployeePass `
    -Group      "Employee" `
    -Region     $Region

New-CognitoTestUser `
    -UserPoolId $UserPoolId `
    -Username   "finance1" `
    -Email      $FinanceEmail `
    -Password   $FinancePass `
    -Group      "FinanceManager" `
    -Region     $Region

Write-Host ""
Write-Host "Test users ready:" -ForegroundColor Green
Write-Host "  Employee        : $EmployeeEmail / $EmployeePass"
Write-Host "  Finance Manager : $FinanceEmail / $FinancePass"
Write-Host ""
Write-Host "Next: run invoke-samples.ps1 to test the live API."
