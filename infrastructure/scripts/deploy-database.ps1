param(
    [string]$environment,
    [string]$sqlServerName
)

Write-Host "Starting deployment of sql server"

Get-AzResource -ResourceType "Microsoft.Sql/servers" | Select-Object -Property Name

$server = Get-AzResource -ResourceType "Microsoft.Sql/servers" -Name $sqlServerName
if ($server.Length -gt 1) {
    Write-Host "Found multiple sql servers using name $sqlServerName"
    $server | ForEach-Object { Write-Host $_.ResourceId }
    throw "Found multiple sql servers..."
}
if ($null -eq $server) {
  throw "Could not locate any sql servers"
}

$fcoreEnv = "test"
if ($environment -eq "fprd") { $fcoreEnv = "prod" }

$sqlServer = Get-AzSqlServer -ResourceGroupName $server.ResourceGroupName -ServerName $server.Name

$ePools = Get-AzSqlElasticPool -ServerName $sqlServer.ServerName -ResourceGroupName $sqlServer.ResourceGroupName
if ($ePools.Length -gt 1) {
    $pool = $ePools | Where-Object { $_.Tags["pool-type"] -eq "main" } | select-object -First 1    
} else {
    $pool = $ePools | Select-Object -First 1
}

New-AzResourceGroupDeployment -Mode Incremental -Name "fusion-app-resources-database-$environment" -ResourceGroupName $server.ResourceGroupName -TemplateFile  "$($env:BUILD_SOURCESDIRECTORY)/infrastructure/arm/database.template.json" `
    -env-name $environment `
    -sqlserver_name $server.Name `
    -sql-elastic-pool-id $pool.ResourceId `
    -fcore-env $fcoreEnv


$connectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=Fusion-Apps-Resources-$environment-DB;Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$summaryConnectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=sqldb-fapp-fra-summary-db-$environment;Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "##vso[task.setvariable variable=SqlConnectionString]$connectionString" 
Write-Host "##vso[task.setvariable variable=SqlDatabaseName]Fusion-Apps-Resources-$environment-DB"

Write-Host "##vso[task.setvariable variable=SummarySqlConnectionString]$summaryConnectionString" 
Write-Host "##vso[task.setvariable variable=SummarySqlDatabaseName]sqldb-fapp-fra-summary-db-$environment"
