param(
    [string]$clientId,
    [string]$sqlServerName,
    [string]$sqlDatabaseName
)

Import-Module SqlServer

if ($sqlServerName -eq "fusion-prod-sqlserver")  {  $sqlPasswordKeyVault = "fusion-env-prod-kv" }
elseif ($sqlServerName -eq "fusion-test-sqlserver") { $sqlPasswordKeyVault = "fusion-env-test-kv" }
else { throw "Unsupported sql server $sqlServerName"}

function Get-SqlServer {
    $server = Get-AzResource -ResourceType "Microsoft.Sql/servers" -Name $sqlServerName
    if ($server.Length -gt 1) {
        Write-Host "Found multiple sql servers using name $serverName"
        $server | ForEach-Object { Write-Host $_.ResourceId }
        throw "Found multiple sql servers..."
    }
    $sqlServer = Get-AzSqlServer -ResourceGroupName $server.ResourceGroupName -ServerName $server.Name
    return $sqlServer
}

function ConvertTo-Sid {
    param (
        [string]$appId
    )
    [guid]$guid = [System.Guid]::Parse($appId)
    foreach ($byte in $guid.ToByteArray()) {
        $byteGuid += [System.String]::Format("{0:X2}", $byte)
    }
    return "0x" + $byteGuid
}

$sqlpasswordSecret = Get-AzKeyVaultSecret -VaultName $sqlPasswordKeyVault -Name fusion-sql-password

$sqlServer = Get-SqlServer 
$sp = Get-AzADApplication -ApplicationId $clientId
$SID = ConvertTo-Sid -appId $clientId
$sql = "CREATE USER [$($sp.DisplayName)] WITH DEFAULT_SCHEMA=[dbo], SID = $SID, TYPE = E;ALTER ROLE db_owner ADD MEMBER [$($sp.DisplayName)];"


Invoke-Sqlcmd -ServerInstance $sqlServer.FullyQualifiedDomainName `
        -Database $sqlDatabaseName `
        -Username $sqlServer.SqlAdministratorLogin `
        -Password $sqlpasswordSecret.SecretValueText `
        -Query $sql `
        -ConnectionTimeout 120