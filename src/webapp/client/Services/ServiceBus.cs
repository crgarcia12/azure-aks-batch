using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Client.Services;
using Shared;

namespace client.Services;

public class ServiceBus
{
    public static int SentMessages = 0;

    private static Random Rand = new Random();
    
    private async Task<ServiceBusClient> GetServiceBusClient()
    {
        string connStr = SecretProvider.GetSecret("ServiceBusConnectionString");
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

    public async Task SendMessages(string SessionId, int Quantity)
    {
        string queueName = Constant.ServiceBusRequestQueueName;

        await using (ServiceBusClient client = await GetServiceBusClient())
        {
            await using (var sender = client.CreateSender(queueName))
            {
                using (ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync())
                {
                    for (int i = 1; i <= Quantity; i++)
                    {
                        SentMessages++;

                        var message = new CalculatorMessage()
                        {
                            BatchId = SessionId,
                            MessageId = i,
                            Digits = Rand.Next(5, 10),
                            ResponseSessionId = Receiver.SessionId,
                            UserId = SessionId,
                            StartProcessingUtc = DateTime.UtcNow,
                        };

                        Console.WriteLine($"Sending message: '{message.ToString()}'-");
                        if (!messageBatch.TryAddMessage(new ServiceBusMessage(message.ToString())))
                        {
                            throw new Exception($"Could not add message to the batch");
                        }
                    }
                    try
                    {
                        // Use the producer client to send the batch of messages to the Service Bus queue
                        await sender.SendMessagesAsync(messageBatch);
                        Console.WriteLine($"A batch of {Quantity} messages has been published to the queue.");
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

