﻿using Azure.Core.Diagnostics;
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
        string connStr = SecretProvider.GetSecret("ServiceBusConnectionString");
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
                await args.CompleteMessageAsync(args.Message);
                var msg = CalculatorMessage.FromJsonString(body);

                PiCalculator piCalculator = new PiCalculator();
                string piresponse = piCalculator.CalculatePi(msg.Digits);
                msg.Response = piresponse;

                await SendResponse(msg);
                
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
                Console.WriteLine($"Responding with message: '{msg.ToString()}'-");
                await sender.SendMessageAsync(msg);
            }
        }
    }
}