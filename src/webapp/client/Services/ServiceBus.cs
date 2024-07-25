using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace client.Services;

public class ServiceBus
{
    private string GetSecret(string secretName, ILogger logger)
    {
        logger.LogInformation($"Getting secret {secretName}");
        string value = Environment.GetEnvironmentVariable(secretName) ?? String.Empty;
        logger.LogInformation($"[Env]Secret '{secretName}' is '{value}'");

        if (string.IsNullOrWhiteSpace(value))
        {
            value = File.ReadAllText("/mnt/secrets/" + secretName);
            logger.LogInformation($"[File]Secret '{secretName}' is '{value}'");
        }

        logger.LogInformation($"[Result]Secret '{secretName}' is '{value}'");
        return value;
    }

    private async Task<ServiceBusClient> GetServiceBusClient(ILogger logger)
    {
        string connStr = GetSecret("ServiceBusConnectionString", logger);
        string queueName = GetSecret("ServiceBusQueueName", logger);

        ServiceBusAdministrationClient serviceBusAdministrationClient = new ServiceBusAdministrationClient(connStr);
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

        return new ServiceBusClient(connStr, clientOptions);
    }

    public async Task SendMessages(ILogger logger, int quantity)
    {
        string queueName = GetSecret("ServiceBusQueueName", logger);

        await using (ServiceBusClient client = await GetServiceBusClient(logger))
        {
            await using (var sender = client.CreateSender(queueName))
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

