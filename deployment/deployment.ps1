# You need Kustomize to run this script.
# Install on windows: choco install Kustomize

# You also need to set the secrets. Copy platform/secrets.template.yaml to platform/secrets.yaml and fill in the values.

param(
    [string] $imageVersion = "040",
    [string] $prefix = "crgar-aks-batch",
    [switch] $delete = $false
)

if ((get-location | split-path -leaf) -ne "deployment" ) { 
    Write-Error "Wrong path. This script must be executed from the deployment folder" -ErrorAction Stop
}

function Get-UtcString() {
    return Get-Date -UFormat "%Y-%m-%dT%H:%M:%SZ"
}

function Execute-Block([string]$blockName, [ScriptBlock]$block) {
    Write-Verbose "[$(Get-UtcString)] Starting $blockName ..." -Verbose
    $stopwatch = [system.diagnostics.stopwatch]::StartNew()
    & $block
    Write-Verbose "[$(Get-UtcString)] Finished $blockName. It took $($stopwatch.Elapsed.TotalMilliseconds) ms" -Verbose
}


$resourceGroup = "$prefix-rg"
$acrName = "$prefix-acr" -Replace "-", ""
$aksName = "$prefix-aks"

$action = "apply"
if($delete) {
    $action = "delete"
}

# Login
Execute-Block "Login" {    
    az aks get-credentials --resource-group $resourceGroup --name $aksName --admin
    az acr login --name $acrName
}

# Deploy platform
Execute-Block "$action platform components" {  
    pushd ./platform  
        kubectl $action -f namespace.yaml
        kubectl $action -f secrets.yaml
    popd
}

# Build worker
$imageName = "$acrName.azurecr.io/queueworker:local-$imageVersion"
if(!$delete) {
    Execute-Block "Building queue worker" {  
        pushd ../src
        docker build -t $imageName -f ./queueworker/queueworker/Dockerfile . --progress=plain --no-cache
        docker push $imageName
        popd
    }
}

Execute-Block "$action queue worker" {  
    pushd ./queueworker
    kustomize edit set image queueworker=$imageName
    kubectl $action -k .
    popd
}

# Build client
$imageName = "$acrName.azurecr.io/client:local-$imageVersion"
if(!$delete) {
    Execute-Block "Building client" {
        pushd ../src
        docker build -t $imageName -f .\webapp\client\Dockerfile . --progress=plain --no-cache
        docker push $imageName
        popd
    }
}

Execute-Block "$action client" {  
    pushd client
    kustomize edit set image client=$imageName
    kubectl $action -k .
    popd
}