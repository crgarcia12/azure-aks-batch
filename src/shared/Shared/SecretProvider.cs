namespace Shared
{
    public class SecretProvider
    {
        public static string GetSecret(string secretName)
        {
            string value = Environment.GetEnvironmentVariable(secretName) ?? String.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                value = File.ReadAllText("/mnt/secrets/" + secretName);
            }

            return value;
        }
    }
}
