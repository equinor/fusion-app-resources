param(
    [string]$applicationId,
    [string]$keyVaultName
)

$SECRET_NAME = "AzureAd--ClientSecret"
$AAD_APP_ID = $applicationId
$SECRETVAULTNAME = $keyVaultName


$secret = Get-AzKeyVaultSecret -VaultName $SECRETVAULTNAME -Name $SECRET_NAME

$generateNew = $false

if ($null -ne $secret.Tags -and $secret.Tags["auto-generated"] -eq "true") {
    Write-Host "Located autogenerated secret"

    ## Check expires flag
    $delta = $secret.Expires - (Get-Date)

    Write-Host "Secret expires in $($delta.Days) days"
    if ($delta.Days -lt 60) {
        $generateNew = $true
    }

} else {
    Write-Host "Secret not autogenerated, creating new..."
    $generateNew = $true
}

if (-not $generateNew) {
    Write-Host "No need to generate secret..."
    return
} else {
    Write-Host "Generating new secret..."
}

## Generate secret on aad app
$passwordString = -join ((48..57) + (65..90) + (97..122) + (33,35) + (36..38) | Get-Random -Count 64 | foreach {[char]$_})
$passwordString = [System.Convert]::ToBase64String([System.Text.UTF8Encoding]::UTF8.GetBytes($passwordString)) 

$password = ConvertTo-SecureString -String $passwordString -AsPlainText -Force
$startDate = Get-Date
$endDate = $startDate.AddMonths(6)
$newSecret = New-AzADAppCredential -Password $password -ApplicationId $AAD_APP_ID -StartDate $startDate -EndDate $endDate

Write-Host "New secret [$($newSecret.KeyId)] generated with expiration date $endDate"

## Add to key vault
Set-AzKeyVaultSecret -VaultName $SECRETVAULTNAME -Name $SECRET_NAME -SecretValue $password -Expires $endDate -Tag @{ "auto-generated" = "true"; "keyId" = $newSecret.KeyId }

