﻿using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Shared;

namespace Client.Services
{
    public static class Receiver
    {
        public static readonly string SessionId = Guid.NewGuid().ToString();
        public static ServiceBusClient client;
        public static ServiceBusSessionProcessor processor;

        public static async Task SetMessageReceiver()
        {
            string connStr = SecretProvider.GetSecret("ServiceBusConnectionString");
            string queueName = Constant.ServiceBusResponseQueueName;

            ServiceBusAdministrationClient serviceBusAdministrationClient = new ServiceBusAdministrationClient(connStr);
            CreateQueueOptions responseQueueOptions = new CreateQueueOptions(queueName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromMinutes(5),
                Name = queueName,
                EnablePartitioning = true,
                RequiresSession = true
            };
            if (!await serviceBusAdministrationClient.QueueExistsAsync(queueName))
            {
                await serviceBusAdministrationClient.CreateQueueAsync(responseQueueOptions);
            }

            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            client = new ServiceBusClient(connStr);

            // create the options to use for configuring the processor
            var options = new ServiceBusSessionProcessorOptions
            {
                AutoCompleteMessages = true,
                MaxConcurrentSessions = 20,

                // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
                // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
                // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
                MaxConcurrentCallsPerSession = 2,

                // Processing can be optionally limited to a subset of session Ids.
                SessionIds = { SessionId },
            };

            // create a session processor that we can use to process the messages
            processor = client.CreateSessionProcessor(queueName, options);

            // configure the message and error event handler to use - these event handlers are required
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;

            // configure optional event handlers that will be executed when a session starts processing and stops processing
            // NOTE: The SessionInitializingAsync event is raised when the processor obtains a lock for a session. This does not mean the session was
            // never processed before by this or any other ServiceBusSessionProcessor instances. Similarly, the SessionClosingAsync
            // event is raised when no more messages are available for the session being processed subject to the SessionIdleTimeout
            // in the ServiceBusSessionProcessorOptions. If additional messages are sent for that session later, the SessionInitializingAsync and SessionClosingAsync
            // events would be raised again.

            processor.SessionInitializingAsync += SessionInitializingHandler;
            processor.SessionClosingAsync += SessionClosingHandler;

            async Task MessageHandler(ProcessSessionMessageEventArgs args)
            {
                CalculatorMessage message = CalculatorMessage.FromJsonString(args.Message.Body.ToString());

                // we can evaluate application logic and use that to determine how to settle the message.
                await args.CompleteMessageAsync(args.Message);

                Console.WriteLine("Received response: " + message.ToString());
            }

            Task ErrorHandler(ProcessErrorEventArgs args)
            {
                Console.WriteLine("Error Receiving message");
                // the error source tells me at what point in the processing an error occurred
                Console.WriteLine(args.ErrorSource);
                // the fully qualified namespace is available
                Console.WriteLine(args.FullyQualifiedNamespace);
                // as well as the entity path
                Console.WriteLine(args.EntityPath);
                Console.WriteLine(args.Exception.ToString());
                return Task.CompletedTask;
            }

            async Task SessionInitializingHandler(ProcessSessionEventArgs args)
            {
                Console.WriteLine("************** Session Initializing Handler **************");
                await args.SetSessionStateAsync(new BinaryData("Some state specific to this session when the session is opened for processing."));
            }

            async Task SessionClosingHandler(ProcessSessionEventArgs args)
            {
                Console.WriteLine("Finish processing responses");
                // We may want to clear the session state when no more messages are available for the session or when some known terminal message
                // has been received. This is entirely dependent on the application scenario.
                BinaryData sessionState = await args.GetSessionStateAsync();
                if (sessionState.ToString() ==
                    "Some state that indicates the final message was received for the session")
                {
                    await args.SetSessionStateAsync(null);
                }
            }

            // start processing
            await processor.StartProcessingAsync();
            Console.WriteLine($"(\"************** Responses procesor IsProcessing: {processor.IsProcessing} IsClosed: {processor.IsClosed}");
            // since the processing happens in the background, we add a Console.ReadKey to allow the processing to continue until a key is pressed.
            Console.ReadKey();
        }
    }
}
