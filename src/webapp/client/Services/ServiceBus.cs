using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace client.Services;

public class ServiceBus
{
    public async Task SendMessages(int quantity)
    {
        ServiceBusAdministrationClient serviceBusAdministrationClient = new ServiceBusAdministrationClient(connstr);
        if (!await serviceBusAdministrationClient.QueueExistsAsync(queueName))
        {
            await serviceBusAdministrationClient.CreateQueueAsync(queueName);
        }




        ServiceBusClient client;
        ServiceBusSender sender;
        var clientOptions = new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        await using (client = new ServiceBusClient(connstr, clientOptions))
        {
            await using (sender = client.CreateSender(queueName))
            {

                using (ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync())
                {
                    for (int i = 1; i <= quantity; i++)
                    {
                        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}")))
                        {
                            throw new Exception($"The message {i} is too large to fit in the batch.");
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

