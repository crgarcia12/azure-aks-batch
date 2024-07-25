param location string
param prefix string
param kubeletidentityId string
param aksId string

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: replace('${prefix}-acr', '-', '')
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}


var acrPullRoleDefinitionGuid = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var acrPullRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleDefinitionGuid)

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, aksId, acrPullRoleDefinitionId)
  scope: acr
  properties: {
    principalId: kubeletidentityId
    roleDefinitionId: acrPullRoleDefinitionId
    principalType: 'ServicePrincipal'
  }
}


output loginServer string = acr.properties.loginServer
