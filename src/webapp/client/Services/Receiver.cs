using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using client.Services;
using Client.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared;
using System.Text.Json;

namespace Client.Services
{
    public class Receiver
    {
        public static int ReceivedMessages = 0;
        public static readonly string SessionId = Guid.NewGuid().ToString();

        private ServiceBusClient? _client;
        private ServiceBusSessionProcessor? _processor;
        private IHubContext<CalculatorHub> _calculatorHub;

        public Receiver(IHubContext<CalculatorHub> calculatorHub)
        {
            _calculatorHub = calculatorHub;
        }

        public async Task SetMessageReceiver()
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
            _client = new ServiceBusClient(connStr);

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
            _processor = _client.CreateSessionProcessor(queueName, options);

            // configure the message and error event handler to use - these event handlers are required
            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            // configure optional event handlers that will be executed when a session starts processing and stops processing
            // NOTE: The SessionInitializingAsync event is raised when the processor obtains a lock for a session. This does not mean the session was
            // never processed before by this or any other ServiceBusSessionProcessor instances. Similarly, the SessionClosingAsync
            // event is raised when no more messages are available for the session being processed subject to the SessionIdleTimeout
            // in the ServiceBusSessionProcessorOptions. If additional messages are sent for that session later, the SessionInitializingAsync and SessionClosingAsync
            // events would be raised again.

            _processor.SessionInitializingAsync += SessionInitializingHandler;
            _processor.SessionClosingAsync += SessionClosingHandler;


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
            await _processor.StartProcessingAsync();
            Console.WriteLine($"(\"************** Responses procesor IsProcessing: {_processor.IsProcessing} IsClosed: {_processor.IsClosed}");
            // since the processing happens in the background, we add a Console.ReadKey to allow the processing to continue until a key is pressed.
            // Console.ReadKey();
        }

        async Task MessageHandler(ProcessSessionMessageEventArgs args)
        {
            CalculatorMessage message = CalculatorMessage.FromJsonString(args.Message.Body.ToString());

            ReceivedMessages++;
            await _calculatorHub.Clients.All.SendAsync("UpdateMessagesReceived", ReceivedMessages);
            await _calculatorHub.Clients.All.SendAsync("UpdateLastCalculationTimeMs", message.CalculationTimeMs);
            await _calculatorHub.Clients.All.SendAsync("UpdateMessagesInQueue", ServiceBus.SentMessages - ReceivedMessages);

            // we can evaluate application logic and use that to determine how to settle the message.
            await args.CompleteMessageAsync(args.Message);

            double totalProcessingTimeMs = (DateTime.UtcNow - message.StartProcessingUtc).TotalMilliseconds;
            await _calculatorHub.Clients.All.SendAsync("ReceiveMessage", "Server", $"[{message.BatchId}][{message.MessageId}][{totalProcessingTimeMs} ms]: {message.Response}");           
            Console.WriteLine("Total Processing Time: " + totalProcessingTimeMs);
            Console.WriteLine("Received response: " + JsonSerializer.Serialize(message));
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
    }
}
