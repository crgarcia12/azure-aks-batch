using Azure.Messaging.ServiceBus;
using client.Models;
using client.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            return View();
        }

        public async Task<IActionResult> SendMessages(string UserID, int Quantity)
        {
            try
            {
                ServiceBus serviceBus = new ServiceBus();
                await serviceBus.SendMessages(UserID, Quantity);
                return RedirectToAction("Privacty");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending messages");
                return RedirectToAction("Index");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
