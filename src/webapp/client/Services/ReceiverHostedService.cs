
using Client.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Client.Services
{
    public class ReceiverHostedService : BackgroundService
    {
        private Receiver _receiver;
        private IHubContext<CalculatorHub> _calculatorHub;

        public ReceiverHostedService(IHubContext<CalculatorHub> calculatorHub)
        {
            _calculatorHub = calculatorHub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("********* ReceiverHostedService is starting.");
            _receiver = new Receiver(_calculatorHub);
            Task receiverTask = _receiver.SetMessageReceiver();
            Console.WriteLine("********* ReceiverHostedService started.");
            return receiverTask;
        }
    }
}
