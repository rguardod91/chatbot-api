namespace ChatBot.Infrastructure.ExternalServices.Tranxa.Models
{
    public class TranxaPerson
    {
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string IdNumber { get; set; } = default!;
        public string DocType { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Since { get; set; } = default!;
        public int IdClient { get; set; }
    }
}
