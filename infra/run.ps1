# az deployment sub create --template-file "main.bicep" --location "eastus"

# # Enalbe workload ideantity for Keda
# https://learn.microsoft.com/en-us/azure/aks/keda-workload-identity

$aks = "crgar-aks-batch-aks"
$rg = "crgar-aks-batch-rg"
$serviceBusName = "crgar-aks-batchsb"

# az aks update `
#     --resource-group $rg `
#     --name $aks `
#     --enable-oidc-issuer `
#     --enable-workload-identity

# Verify the configuration
# az aks show `
#     --name $aks `
#     --resource-group $rg `
#     --query "[workloadAutoScalerProfile, securityProfile, oidcIssuerProfile]"

# # Create a managed identity
# $miName = "crgar-aks-batch-mi"
# $miClientId=$(az identity create --name $miName --resource-group $rg --query "clientId" --output tsv)
# $aksOidcIssuer=$(az aks show --name $aks --resource-group $rg --query oidcIssuerProfile.issuerUrl --output tsv)
# $fedWorkload="crgar-aks-batch-fed"
# az identity federated-credential create --name $fedWorkload --identity-name $miName --resource-group $rg --issuer $aksOidcIssuer --subject system:serviceaccount:default:$miName --audience api://AzureADTokenExchange

# $fedKeda="crgar-aks-batch-keda-fed"
# az identity federated-credential create --name $fedKeda --identity-name $miName --resource-group $rg --issuer $aksOidcIssuer --subject system:serviceaccount:kube-system:keda-operator --audience api://AzureADTokenExchange

# # Add Role Assignment
# $miObjectId=$(az identity show --name $miName --resource-group $rg --query "principalId" --output tsv)
# $serviceBusId=$(az servicebus namespace show --name $serviceBusName --resource-group $rg --query "id" --output tsv)
# az role assignment create --role "Azure Service Bus Data Owner" --assignee-object-id $miObjectId --assignee-principal-type ServicePrincipal --scope $serviceBusId

# # Restart keda operator - THIS WILL WAIT UNTIL YOU BREAK WITH CTRL+C
# Write-Warning "Restarting Keda Operator - YOU NEED TO CONTROL+C TO STOP"
# kubectl rollout restart deploy keda-operator -n kube-system
# kubectl get pod -n kube-system -lapp=keda-operator -w

# Write-Warning "Validate the environment variables are there. Look for AZURE_AUTHORITY_HOST, AZURE_FEDERATED_TOKEN_FILE, etc"
# $kedaPodId=$(kubectl get po -n kube-system -l app.kubernetes.io/name=keda-operator -ojsonpath='{.items[0].metadata.name}')
# kubectl describe po $kedaPodId -n kube-system

# $kedaTrigger = @"
# apiVersion: keda.sh/v1alpha1
# kind: TriggerAuthentication
# metadata:
#   name: azure-servicebus-auth
#   namespace: calculator
# spec:
#   podIdentity:
#     provider:  azure-workload
#     identityId: $miClientId
# "@
# $kedaTrigger | kubectl apply -f -