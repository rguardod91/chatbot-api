namespace ChatBot.Infrastructure.Security
{
    public static class DataMaskingService
    {
        public static string MaskDocument(string document)
        {
            if (string.IsNullOrWhiteSpace(document) || document.Length < 4)
                return "****";

            return new string('*', document.Length - 4) + document[^4..];
        }

        public static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4)
                return "****";

            return new string('*', phone.Length - 4) + phone[^4..];
        }
    }
}
