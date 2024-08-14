param(
    [string]$environment,
    [string]$apiAppRegistrationClientId
)

$resourceGroup = "fusion-apps-resources-$environment"
$envKeyVault = "kv-fap-resources-$environment"

## Grant the current deploying service principal access

Write-Host "Setting service principal key vault access"
$spName = (Get-AzContext).Account.Id
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ServicePrincipalName $spName -PermissionsToSecrets get,list,set,delete

## LEGACY - Need to grant permissions to the core app registration (VisualStudioSPNb8d5119a-cbba-4511-bc05-d9d9cd034c77)
## This should be removed when all pipelines have been updated
Write-Host "Setting LEGACY VS service principal access"
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ObjectId b3fcda7c-7d40-4e46-bc29-be0fb4c1506f -PermissionsToSecrets get,list,set,delete


## Grant permission to the app registration specified in params.
## This should be the one acting as the api.

Write-Host "Setting ad app service principal key vault access"
$appSpId = (Get-AzADServicePrincipal -ApplicationId $apiAppRegistrationClientId).Id
Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ObjectId $appSpId -PermissionsToSecrets get,list




## Grant all web apps with identity get & list permissions in the vault.
## This is needed so we do not remove them when running infra deploy

$webApps = Get-AzWebApp -ResourceGroupName $resourceGroup
foreach ($webApp in $webApps) {
    if ($null -ne $webApp.Identity) {
        Write-Host "Granting GET,LIST permission to web app $($webApp.Name)"
        Set-AzKeyVaultAccessPolicy -VaultName $envKeyVault -ResourceGroupName $resourceGroup -ObjectId $webApp.Identity.PrincipalId -PermissionsToSecrets get,list
    }
}