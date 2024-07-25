# You need Kustomize to run this script.
# Install on windows: choco install Kustomize

$prefix = "crgar-aks-batch"
$resourceGroup = "$prefix-rg"
$acrName = "$prefix-acr" -Replace "-", ""
$aksName = "$prefix-aks"

$imageVersion = "003"

# Login
az aks get-credentials --resource-group $resourceGroup --name $aksName --admin
az acr login --name $acrName

# Deploy secrets
pushd .\secrets
kubectl apply -f secrets.yaml
popd

# Build worker
pushd ..\src\queueworker\queueworker
$imageName = "$acrName.azurecr.io/queueworker:local-$imageVersion"
docker build -t $imageName .
docker push $imageName
popd

pushd queueworker
kustomize edit set image queueworker=$imageName
kubectl apply -k .
popd


# Build client
pushd ..\src\webapp\client
$imageName = "$acrName.azurecr.io/client:local-$imageVersion"
docker build -t $imageName .
docker push $imageName
popd

pushd client
kustomize edit set image client=$imageName
kubectl apply -k .
popd
