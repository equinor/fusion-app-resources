param(
    [string]$environment,
    [string]$clientId
)

Write-Host "Starting deployment of general resources"

$resourceGroup = "fusion-apps-resources-$environment"
$envKeyVault = "kv-fap-resources-$environment"

Write-Host "Using resource group $resourceGroup"      


$clientSecretName = "ClientSecret-Resources-Test"
$adClientSecret = Get-AzKeyVaultSecret -VaultName ProView-Shared-Secrets -Name $clientSecretName

Write-Host "Deploying template"

New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-resources-environment" -ResourceGroupName $resourceGroup -TemplateFile  "$($env:BUILD_SOURCESDIRECTORY)/src/backend/infrastructure/arm-templates/environment.template.json" `
    -env-name $environment `
    -aad-client-secret $adClientSecret.SecretValue `
    -sql-connection-string $env:SQLCONNECTIONSTRING `
    -create-hosting-plan ($environment -eq "fprd")

Write-Host "Setting service principal key vault access"
$spName = (Get-AzContext).Account.Id
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ServicePrincipalName $spName -PermissionsToSecrets get,list,set

Write-Host "Setting ad app service principal key vault access"
$appSpId = (Get-AzADServicePrincipal -ApplicationId $clientId).Id
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ObjectId $appSpId -PermissionsToSecrets get,list