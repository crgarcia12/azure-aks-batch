using Azure.Core.Diagnostics;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Serilog;
using Shared;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text.Json;

namespace queueworker;

public class ServiceBus
{
    private readonly string PodName;
    private readonly string NodeName;
    
    ServiceBusProcessor? processor;
 
    private bool MakeSlower()
    {
        try
        {
            if (Environment.GetEnvironmentVariable("MAKE_SLOWER").Contains("1")) 
            {
                return true;
            }                
        }
        catch(Exception ex)
        {
        }
        return false;
    }

    public ServiceBus()
    {
        PodName = Environment.GetEnvironmentVariable("POD_NAME") ?? "Local";
        NodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? "Local";
    }

    private void Log(string message)
    {
        Console.WriteLine($"[{NodeName}][{PodName}]" + message);
    }

    public void LogProcessorState()
    {
        if (processor == null)
        {
            Log($"Processor not initialized");
            return;
        }
        Log($"Processor - IsProcessing:{processor.IsProcessing} IsClosed:{processor.IsClosed}");
    }

    public async Task ProcessMessagesAsync()
    {
        string connStr = SecretProvider.GetSecret("ServiceBusConnectionString");
        string queueName = Constant.ServiceBusRequestQueueName;

        Log("Starting the queue client");

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
            MaxConcurrentCalls = 1,
        };

        processor = client.CreateProcessor(queueName, sbProcessorOptions);
        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                string body = args.Message.Body.ToString();
                await args.CompleteMessageAsync(args.Message);
                var msg = CalculatorMessage.FromJsonString(body);

                Log($"[{msg.BatchId}][{msg.MessageId}] Start processing message");
                PiCalculator piCalculator = new PiCalculator();
                string piresponse = piCalculator.CalculatePi(msg.Digits);
                msg.Response = piresponse;
                msg.CalculationTimeMs = sw.ElapsedMilliseconds;

                if(MakeSlower())
                {
                    Log($"[{msg.BatchId}][{msg.MessageId}] Warning: This is a very lazy calculator");
                    await Task.Delay(5000);
                }
                await SendResponse(msg);
                Log($"[{msg.BatchId}][{msg.MessageId}] Finish processing message. It Took [{sw.ElapsedMilliseconds}] Ms");
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

    private async Task<ServiceBusClient> GetServiceBusClient()
    {
        string connStr = SecretProvider.GetSecret("ServiceBusConnectionString");
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
                var msg = new ServiceBusMessage(message.ToString())
                {
                    SessionId = message.ResponseSessionId
                };
                Log($"Responding with message: '{JsonSerializer.Serialize(msg)}'-");
                await sender.SendMessageAsync(msg);
            }
        }
    }
}