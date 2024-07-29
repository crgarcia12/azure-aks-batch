using System.Text.Json;

namespace Shared
{
    [Serializable]
    public class CalculatorMessage
    {
        public int BatchId { get; set; }
        public int MessageId { get; set; }
        public int Digits { get; set; }
        public string ResponseSessionId { get; set; } = String.Empty;
        public int Response { get; set; }


        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public static CalculatorMessage FromJsonString(string message)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            CalculatorMessage? response = JsonSerializer.Deserialize<CalculatorMessage>(message);
            if(response == null)
            {
                throw new ArgumentNullException("Could not deserialize");
            }

            return response;
        }
    }


}
