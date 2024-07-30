﻿using client.Services;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Client.SignalR;

public class CalculatorHub : Hub
{
    public async Task Calculate(string user, string message)
    {
        int nrOfMessages = int.Parse(message);
        ServiceBus serviceBus = new ServiceBus();
        await serviceBus.SendMessages(user, nrOfMessages);
        await Clients.All.SendAsync("CalculationStarted");
    }

    public async Task SendToClient(string user, CalculatorMessage message)
    {
        await Clients.All.SendAsync("ReceiveMessage", "Server", $"[{message.BatchId}][{message.MessageId}]: {message.Response}");
    }
}