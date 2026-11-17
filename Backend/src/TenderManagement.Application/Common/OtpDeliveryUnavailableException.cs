namespace TenderManagement.Application.Common
{
    public sealed class OtpDeliveryUnavailableException : Exception
    {
        public OtpDeliveryUnavailableException()
            : base("One-time code delivery is not configured for this environment.")
        {
        }
    }
}
