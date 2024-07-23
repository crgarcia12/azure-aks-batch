param prefix string
param location string

resource batchAksCluster 'Microsoft.ContainerService/managedClusters@2024-02-01' = {
  name: '${prefix}-aks'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    workloadAutoScalerProfile: {
      keda: {
          enabled: true
      }
    }
    dnsPrefix: '${prefix}-dns'
    networkProfile: {
      loadBalancerSku: 'standard'
      networkPlugin: 'azure'
    }
    addonProfiles: {
      httpApplicationRouting: {
        enabled: true
      }
    }
    agentPoolProfiles: [
      {
        name: 'systempool'
        count: 1
        vmSize: 'Standard_D2s_v3'
        osType: 'Linux'
        mode: 'System'
      },{
        name: 'userpool'
        enableAutoScaling: true
        minCount: 1
        maxCount: 4
        count: 1
        // osDiskType: 'Ephemeral'
        vmSize: 'Standard_D2s_v3'
        osType: 'Linux'
        mode: 'User'
      }
    ]
    // autoUpgradeProfile: {
    //   nodeOSUpgradeChannel: 'NodeImage'
    //   upgradeChannel: 'NodeImage'
    // }
    azureMonitorProfile: {
      metrics: {
        enabled: true
        // kubeStateMetrics: {
        //   metricAnnotationsAllowList: 'string'
        //   metricLabelsAllowlist: 'string'
        // }
      }
    }
    ingressProfile: {
      webAppRouting: {
        // dnsZoneResourceIds: [
        //   'string'
        // ]
        enabled: true
      }
    }
    // metricsProfile: {
    //   costAnalysis: {
    //     enabled: true
    //   }
    // }
  }
}

output aksName string = batchAksCluster.name
