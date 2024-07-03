param(
    [string]$environment,
    [string]$clientId
)

Write-Host "Starting deployment of general resources"

$resourceGroup = "fusion-apps-resources-$environment"
$envKeyVault = "kv-fap-resources-$environment"

Write-Host "Using resource group $resourceGroup"      


Write-Host "Deploying template"

New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-resources-environment" -ResourceGroupName $resourceGroup -TemplateFile  "$($env:BUILD_SOURCESDIRECTORY)/infrastructure/arm/environment.template.json" `
    -env-name $environment `
    -sql-connection-string $env:SQLCONNECTIONSTRING

Write-Host "Setting service principal key vault access"
$spName = (Get-AzContext).Account.Id
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ServicePrincipalName $spName -PermissionsToSecrets get,list,set,delete

Write-Host "Setting ad app service principal key vault access"
$appSpId = (Get-AzADServicePrincipal -ApplicationId $clientId).Id
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ObjectId $appSpId -PermissionsToSecrets get,list

