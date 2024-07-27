using Azure.Messaging.ServiceBus;

namespace Client.Services
{
    public class SecretProvider
    {
        public static string GetSecret(string secretName, ILogger? logger)
        {
            logger?.LogInformation($"Getting secret {secretName}");
            string value = Environment.GetEnvironmentVariable(secretName) ?? String.Empty;
            logger?.LogInformation($"[Env]Secret '{secretName}' is '{value}'");

            if (string.IsNullOrWhiteSpace(value))
            {
                value = File.ReadAllText("/mnt/secrets/" + secretName);
                logger?.LogInformation($"[File]Secret '{secretName}' is '{value}'");
            }

            logger?.LogInformation($"[Result]Secret '{secretName}' is '{value}'");
            return value;
        }
    }
}
