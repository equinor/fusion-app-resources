param(
    [string]$environment
)

Write-Host "Ensuring resource group exists"

$resourceGroup = "fusion-apps-resources-$environment"

Write-Host "Using resource group $resourceGroup"      

## ENSURE RESOURCE GROUP
$rg = Get-AzResourceGroup -Name $resourceGroup -ErrorAction SilentlyContinue
$rgTags = @{"fusion-app" = "resources"; "fusion-app-env" = $environment; "fusion-app-component" = "resources-rg-$environment" }
if ($null -eq $rg) {
  Write-Host "Creating new resource group"
  New-AzResourceGroup -Name $resourceGroup -Location northeurope
}
Set-AzResourceGroup -Name $resourceGroup -Tag $rgTags