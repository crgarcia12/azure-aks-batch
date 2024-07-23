targetScope = 'subscription'

// Define parameters for resource group
param location string = 'eastus'
param prefix string = 'crgar-aks-batch'

// Define parameters for AKS
var resourceGroupName = '${prefix}-rg'

// Resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// AKS cluster
module aks './modules/aks.bicep' = {
  scope: resourceGroup
  name: 'aks'
  params: {
    location: location
    prefix: prefix
  }
  dependsOn: [
    resourceGroup
  ]
} 

module servicebus './modules/servicebus.bicep' = {
  scope: resourceGroup
  name: 'servicebus'
  params: {
    location: location
    prefix: prefix
  }
  dependsOn: [
    resourceGroup
  ]
}

// module prometheus './modules/prometheus.bicep' = {
//   scope: resourceGroup
//   name: 'prometheus'
//   params: {
//     location: location
//     aksName: aks.outputs.aksName
//     prefix: prefix
//   }
//   dependsOn: [
//     resourceGroup
//   ]
// }

// module grafana './modules/grafana.bicep' = {
//   scope: resourceGroup
//   name: 'grafana'
//   params: {
//     location: location
//     prefix: prefix
//     managedPrometheusId: prometheus.outputs.id
//   }
//   dependsOn: [
//     resourceGroup
//   ]
// }
