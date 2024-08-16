#
# Idempotent script to ensure relevant baseline-infrastructure is available
# 
# This script should be safe to execute multiple times.
# App owners are not removed, only appended.
# 
# App registrations should be created be a normal user. 
# - Required roles will differ, as non-prod and prod targets different external apis.
# 
# 
# User running the script must have permissions
# - Owner on subscription -> to set roles
# - Owner on app registrations -> to update 
# - Owner on service principals connected to app registrations
#
# https://github.com/equinor/fusion-core-services/pull/969/files

param(
    [switch]$ServicePrincipals
)

function Configure-DevOps-AppRegistration($appReg, $owners) {
    Write-Host "Patching [$($appReg.name)]"
    Write-Host (ConvertTo-Json $appReg)
    az rest -m patch -u "https://graph.microsoft.com/v1.0/applications/$($appReg.objectId)" `
      --headers 'Content-Type=application/json' `
      --body "@$PSScriptRoot\app-registrations\$($appReg.configFile)"

    $existingServicePrincipalOwners = (az ad sp owner list --id $appReg.managedIdentity) | ConvertFrom-Json
    $existingServicePrincipalOwners

    Write-Host "Setting owners"
    foreach ($owner in $owners) {
        Write-Host "- Adding [$($owner.upn)]"
        az ad app owner add --id $appReg.appId --owner-object-id $owner.objectId

        if ($existingServicePrincipalOwners | Where-Object { $_.id -eq $owner.objectId }) {
            Write-Host "- User is already owner on service principal"
        } else {
            Write-Host "- Adding [$($owner.upn)] to service principal owner list"
         
            ## Construct the payload.
            ## Shitty stuff.. doublequotes must be escaped for the json to be valid.
            $ownerData = @{ "owners@odata.bind" = @("https://graph.microsoft.com/v1.0/users/$($owner.objectId)") } | ConvertTo-Json -Compress
            
            az rest -m patch -u "https://graph.microsoft.com/v1.0/servicePrincipals/$($appReg.managedIdentity)" `
                --headers 'Content-Type=application/json' `
                --body $ownerData.Replace("`"", "\`"")
        }        
    }
}

# Load infra config.
$infraConfig = ConvertFrom-Json (Get-Content -Raw "$PSScriptRoot\config.json")

## 
## Patch app regs to match the defined json in config files `app-registrations/*`.
## - Name, description, CI ++
## - Api permissions
## Adds owners 
## 
foreach  ($appReg in @($infraConfig.spAppRegs.nonProduction, $infraConfig.spAppRegs.production)) {
    Configure-DevOps-AppRegistration `
        -appReg $appReg `
        -owners $infraConfig.appRegOwners
}

if ($ServicePrincipals.IsPresent) {
    return
}

##
## Subscription permission setup
## Add roles to resource groups based on config json.
##
foreach ($roleAssignment in $infraConfig.resourceGroups) {
    Write-Host "Creating role assignment [$($roleAssignment.roleName)] for rg [$($roleAssignment.name)] to sp [$($roleAssignment.managedIdentity)]"
    az role assignment create `
        --assignee $roleAssignment.managedIdentity `
        --role $roleAssignment.roleName `
        --scope "/subscriptions/$($infraConfig.subscriptionId)/resourceGroups/$($roleAssignment.name)"
}

