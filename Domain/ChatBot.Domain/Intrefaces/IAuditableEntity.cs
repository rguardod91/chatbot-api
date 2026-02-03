namespace ChatBot.Domain.Intrefaces
{
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
    }
}
