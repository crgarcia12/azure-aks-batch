//var receiver = client.CreateReceiver(queueName);
//var message = await receiver.ReceiveMessageAsync();
//Console.WriteLine(message.Body);
//await receiver.CompleteMessageAsync(message);

using queueworker;


ServiceBus serviceBusService = new ServiceBus();
await serviceBusService.ProcessMessagesAsync();

// Processing is happening in the background, we need to wait
while (true)
{
    serviceBusService.LogProcessorState();
    await Task.Delay(10000);
}