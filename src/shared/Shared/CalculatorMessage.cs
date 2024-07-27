namespace Shared
{
    [Serializable]
    public class CalculatorMessage
    {
        public int BatchId;
        public int MessageId;
        public int Digits;
        public string ResponseSessionId = String.Empty;
        public int Response;
    }
}
