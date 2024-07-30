using client.Services;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Client.SignalR;

public class CalculatorHub : Hub
{
    public async Task Calculate(string sessionId, string message)
    {
        int nrOfMessages = int.Parse(message);
        ServiceBus serviceBus = new ServiceBus();
        await serviceBus.SendMessages(sessionId, nrOfMessages);
        await Clients.All.SendAsync("CalculationStarted");
        await Clients.All.SendAsync("UpdateMessagesSent", nrOfMessages);
    }

    public async Task SendToAllClient(string sessionId, CalculatorMessage message)
    {
        await Clients.All.SendAsync("ReceiveMessage", "Server", $"[{message.BatchId}][{message.MessageId}]: {message.Response}");
    }
}