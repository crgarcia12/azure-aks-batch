//var receiver = client.CreateReceiver(queueName);
//var message = await receiver.ReceiveMessageAsync();
//Console.WriteLine(message.Body);
//await receiver.CompleteMessageAsync(message);

using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Core.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Azure;
using worker.Services;
using queueworker;


ServiceBusService serviceBusService = new ServiceBusService();
var processor = await serviceBusService.ProcessMessagesAsync();

// Processing is happening in the background, we need to wait
while (true)
{
    Console.WriteLine($"Processor - IsProcessing:{processor.IsProcessing} IsClosed:{processor.IsClosed}");
    await Task.Delay(10000);
}