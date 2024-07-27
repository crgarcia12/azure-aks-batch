using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Client.Services;
using Shared;

namespace client.Services;

public class ServiceBus
{
    private static int BatchId = 0;
    private static Random Rand = new Random();

    private async Task<ServiceBusClient> GetServiceBusClient(ILogger logger)
    {
        string connStr = SecretProvider.GetSecret("ServiceBusConnectionString", logger);
        string queueName = Constant.ServiceBusRequestQueueName;

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

    public async Task SendMessages(ILogger logger, int quantity)
    {
        string queueName = Constant.ServiceBusRequestQueueName;

        await using (ServiceBusClient client = await GetServiceBusClient(logger))
        {
            await using (var sender = client.CreateSender(queueName))
            {
                using (ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync())
                {
                    for (int i = 1; i <= quantity; i++)
                    {
                        var message = new CalculatorMessage()
                        {
                            BatchId = BatchId,
                            MessageId = i,
                            Digits = Rand.Next(1, 1000),
                            ResponseSessionId = Receiver.SessionId
                        };


                        if (!messageBatch.TryAddMessage(new ServiceBusMessage(BinaryData.FromObjectAsJson<CalculatorMessage>(message))))
                        {
                            throw new Exception($"Could not add message to the batch");
                        }
                    }
                    try
                    {
                        // Use the producer client to send the batch of messages to the Service Bus queue
                        await sender.SendMessagesAsync(messageBatch);
                        Console.WriteLine($"A batch of {quantity} messages has been published to the queue.");
                    }
                    finally
                    {
                        // Calling DisposeAsync on client types is required to ensure that network
                        // resources and other unmanaged objects are properly cleaned up.
                    }
                }
            }
        }
    }
}

