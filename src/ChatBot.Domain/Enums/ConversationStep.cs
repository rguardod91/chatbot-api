namespace ChatBot.Domain.Enums
{
    public enum ConversationStep
    {
        Start = 1,
        MainMenu = 2,
        WaitingForDocumentType = 3,
        WaitingForDocumentNumber = 4,
        ValidatingUser = 5,
        SelectProduct = 6,
        AuthenticatedMenu = 7,
        BalanceFlow = 8,
        MovementsFlow = 9,
        BlockFlow = 10
    }
}
