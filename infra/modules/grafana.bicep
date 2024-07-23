param location string
param prefix string
param managedPrometheusId string

resource managedGrafana 'Microsoft.Dashboard/grafana@2023-09-01' =  {
  name: '${prefix}-grafana'
  location: location
  sku: {
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    apiKey: 'Enabled'
    autoGeneratedDomainNameLabelScope: 'TenantReuse'
    grafanaIntegrations: {
      azureMonitorWorkspaceIntegrations: [{
        azureMonitorWorkspaceResourceId: managedPrometheusId
      }]
    }
    publicNetworkAccess: 'Enabled'
  }
}
