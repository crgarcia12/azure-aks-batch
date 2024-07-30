namespace Shared
{
    public class SecretProvider
    {
        public static string GetSecret(string secretName)
        {
            string value = File.ReadAllText("/mnt/secrets/" + secretName);

            return value;
        }
    }
}
