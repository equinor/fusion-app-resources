param(
    [string]$environment
)

Write-Host "Starting deployment of general resources"

$resourceGroup = "fusion-apps-resources-$environment"
$envKeyVault = "kv-fap-resources-$environment"

Write-Host "Using resource group $resourceGroup"      


Write-Host "Deploying template"

New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-resources-environment" -ResourceGroupName $resourceGroup -TemplateFile  "$($env:BUILD_SOURCESDIRECTORY)/infrastructure/arm/environment.template.json" `
    -env-name $environment `
    -sql-connection-string $env:SQLCONNECTIONSTRING `
    -summary-sql-connection-string $env:SUMMARYSQLCONNECTIONSTRING
