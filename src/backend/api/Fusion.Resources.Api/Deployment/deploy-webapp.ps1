param(
    [string]$environment,
    [string]$fusionEnvironment,
    [string]$clientId,
    [string]$clientSecretName,
    [string]$imageName
)

Write-Host "Starting deployment of general resources"

$resourceGroup = "fusion-apps-resources-$environment"
$envKeyVault = "kv-fap-resources-$environment"

Write-Host "Using resource group $resourceGroup"      


$adClientSecret = Get-AzKeyVaultSecret -VaultName $envKeyVault -Name "AzureAd--ClientSecret"
$acrPullToken = Get-AzKeyVaultSecret -VaultName $envKeyVault -Name "ACR-PullToken"

## 
## ACR Pull secret
## 
## The ACR password must be generated on the fusioncr resource and added to the env key vault. 
## No automatic generation of this for now.

$dockerCredentials = @{ username="Resources-fprd-pull"; password=$acrPullToken.SecretValueText }
$dockerInfo = @{
    url = "https://fusioncr.azurecr.io"
    image = $imageName
    startupCommand = ""
}

Write-Host "Deploying template"

$templateFile = "$($env:BUILD_SOURCESDIRECTORY)/src/backend/api/Fusion.Resources.Api/Deployment/webapp.template.json"
New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-resources-webapp" -ResourceGroupName $resourceGroup -TemplateFile $templateFile `
    -env-name $environment `
    -fusion-env-name $fusionEnvironment `
    -clientsecret-secret-id $adClientSecret.Id `
    -client-id $clientId `
    -docker-credentials $dockerCredentials `
    -docker $dockerInfo

