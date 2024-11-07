param(
    [string]$environment,
    [string]$fusionEnvironment,
    [string]$clientId,
    [string]$imageName
)

Write-Host "Starting deployment of web app hosting resources"

###
## Must create new resource group for hosting, due to limitation of having linux and windows hosting plan in same. 
## Windows is the consumption function app - which was suggested for consumtion based due to limitation in linux.
##
function New-HostingResource {
    $resourceGroupName = "fusion-apps-resources-hosting"

    Write-Host "Using resource group $resourceGroupName"
    Write-Host "Ensuring resource group exists"

    ## ENSURE RESOURCE GROUP
    $rg = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
    $rgTags = @{"fusion-app" = "resources"; "fusion-app-env" = "shared"; "fusion-app-component" = "resources-rg-hosting" }
    if ($null -eq $rg) {
        Write-Host "Creating new resource group"
        New-AzResourceGroup -Name $resourceGroupName -Location northeurope
    }
    Set-AzResourceGroup -Name $resourceGroupName -Tag $rgTags

    Write-Host "Deploying hosting infra"
    $templateFile = "$($env:BUILD_SOURCESDIRECTORY)/src/Fusion.Summary.Api/Deployment/hosting.template.json"
    New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-resources-hosting" -ResourceGroupName $resourceGroupName -TemplateFile $templateFile 

    $plan = Get-AzAppServicePlan -ResourceGroupName $resourceGroupName -Name asp-fap-resources-prod
    return $plan
}


$resourceGroup = "fusion-apps-resources-$environment"
$envKeyVault = "kv-fap-resources-$environment"

$hostingPlan = New-HostingResource 


Write-Host "Using resource group $resourceGroup"
$dockerInfo = @{
    url = "https://crfsharedhostingall.azurecr.io"
    image = $imageName
    startupCommand = ""
}


Write-Host "Deploying template"

$templateFile = "$($env:BUILD_SOURCESDIRECTORY)/src/Fusion.Summary.Api/Deployment/webapp.template.json"
New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-summary-webapp" -ResourceGroupName $resourceGroup -TemplateFile $templateFile `
    -env-name $environment `
    -fusion-env-name $fusionEnvironment `
    -clientsecret-secret-id "https://$envKeyVault.vault.azure.net:443/secrets/AzureAd--ClientSecret" `
    -client-id $clientId `
    -docker $dockerInfo `
    -hosting @{ name = $hostingPlan.Name; id = $hostingPlan.Id }

