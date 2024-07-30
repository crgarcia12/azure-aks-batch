using System.Text.Json;

namespace Shared
{
    [Serializable]
    public class CalculatorMessage
    {
        public DateTime StartProcessingUtc { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public int Digits { get; set; }
        public string ResponseSessionId { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public long CalculationTimeMs { get; set; }

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
