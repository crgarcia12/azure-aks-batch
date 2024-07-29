using Shared;

namespace Client.SignalR
{
    public interface ICalculatorHub
    {
        public Task Calculate(string user, string message);

        public Task SendToClient(string user, CalculatorMessage message);
    }
}