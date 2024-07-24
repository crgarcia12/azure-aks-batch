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

Console.WriteLine("Starting the queue client");

// Set the logger, otherwise the processor will eat the exceptions
using AzureEventSourceListener listener =
    AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);


var sbClientOptions = new ServiceBusClientOptions
{
    TransportType = ServiceBusTransportType.AmqpWebSockets
};

var client = new ServiceBusClient(connstr, sbClientOptions);

// create the options to use for configuring the processor
var sbProcessorOptions = new ServiceBusProcessorOptions
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 2,
};

await using ServiceBusProcessor processor = client.CreateProcessor(queueName, sbProcessorOptions);
processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

async Task MessageHandler(ProcessMessageEventArgs args)
{
    try
    {
        string body = args.Message.Body.ToString();
        Console.WriteLine(body);
        await args.CompleteMessageAsync(args.Message);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    // the error source tells me at what point in the processing an error occurred
    Console.WriteLine(args.ErrorSource);
    // the fully qualified namespace is available
    Console.WriteLine(args.FullyQualifiedNamespace);
    // as well as the entity path
    Console.WriteLine(args.EntityPath);
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

// Processing is happening in the background, we need to wait
while (true)
{
    Console.WriteLine($"Processor - IsProcessing:{processor.IsProcessing} IsClosed:{processor.IsClosed}");
    await Task.Delay(10000);
    Console.ReadKey();
}