namespace ChatBot.Domain.ValueObjects
{
    public class PhoneNumber
    {
        public string? Value { get; private set; }

        private PhoneNumber()
        {
            Value = null!; // EF
        }

        public PhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
                throw new ArgumentException("Invalid phone number");

            Value = value;
        }

        public override string ToString() => Value ?? string.Empty;

        public static implicit operator string(PhoneNumber phone) => phone.Value ?? string.Empty;
        public static explicit operator PhoneNumber(string value) => new(value);
    }
}
