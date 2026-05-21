#!/usr/bin/env pwsh
# deploy.ps1 - Build and deploy the Expense Tracker backend to AWS via SAM
#
# Prerequisites:
#   - AWS CLI configured (aws configure)
#   - SAM CLI installed (https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)
#   - .NET 10 SDK installed
#
# Usage:
#   ./deploy.ps1
#   ./deploy.ps1 -Environment prod -Region eu-west-1
#   ./deploy.ps1 -Guided   # interactive first-time setup

param(
    [string]$Environment = "dev",
    [string]$Region = "eu-west-1",
    [string]$StackName = "",
    [switch]$Guided
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($StackName)) {
    $StackName = "expense-tracker-$Environment"
}

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$InfraDir   = Split-Path -Parent $ScriptDir
$TemplateFile = Join-Path $InfraDir "cloudformation" "template.yaml"

Write-Host ""
Write-Host "=== Expense Tracker — SAM Deploy ===" -ForegroundColor Cyan
Write-Host "  Environment : $Environment"
Write-Host "  Stack       : $StackName"
Write-Host "  Region      : $Region"
Write-Host "  Template    : $TemplateFile"
Write-Host ""

# ── 1. Build ────────────────────────────────────────────────────────────────
Write-Host "[1/3] Building Lambda package with SAM..." -ForegroundColor Yellow
sam build `
    --template-file $TemplateFile `
    --build-dir (Join-Path $InfraDir ".aws-sam" "build")

# ── 2. Deploy ───────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/3] Deploying CloudFormation stack '$StackName'..." -ForegroundColor Yellow

$DeployArgs = @(
    "deploy"
    "--template-file", (Join-Path $InfraDir ".aws-sam" "build" "template.yaml")
    "--stack-name", $StackName
    "--region", $Region
    "--capabilities", "CAPABILITY_IAM", "CAPABILITY_NAMED_IAM"
    "--parameter-overrides", "Environment=$Environment"
)

if ($Guided) {
    $DeployArgs += "--guided"
} else {
    $DeployArgs += "--no-confirm-changeset"
    $DeployArgs += "--no-fail-on-empty-changeset"
}

sam @DeployArgs

# ── 3. Outputs ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[3/3] Stack outputs:" -ForegroundColor Yellow
aws cloudformation describe-stacks `
    --stack-name $StackName `
    --region $Region `
    --query "Stacks[0].Outputs[*].{Key:OutputKey,Value:OutputValue}" `
    --output table

Write-Host ""
Write-Host "Deployment complete." -ForegroundColor Green
Write-Host "Next: run seed-users.ps1 with the UserPoolId shown above."
