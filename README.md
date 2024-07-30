# azure-aks-batch

```
ContainerLogV2
| where TimeGenerated > ago(15m)
| where LogMessage contains "Finish processing message"
| extend ProcessingTime = toint(split(split(LogMessage, "[")[5],"]")[0])
| summarize processingcount = count() by bin(TimeGenerated, 1m), PodName
| project TimeGenerated, PodName, processingcount 
| render timechart 
```
```
ContainerLogV2
| where TimeGenerated > ago(15m)
| where LogMessage contains "Finish processing message"
| extend ProcessingTime = toint(split(split(LogMessage, "[")[5],"]")[0])
| project TimeGenerated, PodName, ProcessingTime
| render timechart 
```