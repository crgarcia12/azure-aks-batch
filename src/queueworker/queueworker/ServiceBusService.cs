using Azure.Core.Diagnostics;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Shared;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace queueworker;

public class ServiceBus
{
    ServiceBusProcessor? processor;
    public string GetProcessorState()
    {
        if (processor == null)
        {
            return "Processor - Not initialized";
        }
        return $"Processor - IsProcessing:{processor.IsProcessing} IsClosed:{processor.IsClosed}";
    }

    public async Task ProcessMessagesAsync()
    {
        string connStr = GetSecret("ServiceBusConnectionString");
        string queueName = Constant.ServiceBusRequestQueueName;

        Console.WriteLine("Starting the queue client");

        // Set the logger, otherwise the processor will eat the exceptions
        using AzureEventSourceListener listener =
            AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);


        var sbClientOptions = new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };

        var client = new ServiceBusClient(connStr, sbClientOptions);

        // create the options to use for configuring the processor
        var sbProcessorOptions = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 100,
        };

        processor = client.CreateProcessor(queueName, sbProcessorOptions);
        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                string body = args.Message.Body.ToString();
                CalculatorMessage msg = BinaryData.FromString(body).ToObjectFromJson<CalculatorMessage>();
                msg.Response = msg.Digits * 2;
                await SendResponse(msg);
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

        await processor.StartProcessingAsync();
        return;
    }

    private string GetSecret(string secretName)
    {
        Console.WriteLine($"Getting secret {secretName}");
        string value = Environment.GetEnvironmentVariable(secretName) ?? String.Empty ;
        Console.WriteLine($"[Env]Secret '{secretName}' is '{value}'");

        if (string.IsNullOrWhiteSpace(value))
        {
            value = File.ReadAllText("/mnt/secrets/" + secretName);
            Console.WriteLine($"[File]Secret '{secretName}' is '{value}'");
        }

        Console.WriteLine($"[Result]Secret '{secretName}' is '{value}'");
        return value;
    }

    private async Task<ServiceBusClient> GetServiceBusClient()
    {
        string connStr = GetSecret("ServiceBusConnectionString");
        string queueName = Constant.ServiceBusResponseQueueName;

        ServiceBusAdministrationClient serviceBusAdministrationClient = new ServiceBusAdministrationClient(connStr);
        if (!await serviceBusAdministrationClient.QueueExistsAsync(queueName))
        {
            await serviceBusAdministrationClient.CreateQueueAsync(queueName);
        }

        var clientOptions = new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };

        return new ServiceBusClient(connStr, clientOptions);
    }

    public async Task SendResponse(CalculatorMessage message)
    {
        string queueName = Constant.ServiceBusResponseQueueName;

        await using (ServiceBusClient client = await GetServiceBusClient())
        {
            await using (var sender = client.CreateSender(queueName))
            {
                var msg = new ServiceBusMessage(BinaryData.FromObjectAsJson<CalculatorMessage>(message))
                {
                    SessionId = message.ResponseSessionId
                };
                await sender.SendMessageAsync(msg);
            }
        }
    }
}