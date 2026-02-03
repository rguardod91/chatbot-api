
using ChatBot.Application.DTOs.Tranza;
using ChatBot.Application.DTOs.Tranza.Models;
using ChatBot.Application.Interfaces.External;
using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Application.Interfaces.Services;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using System.Text;

namespace ChatBot.Infrastructure.Services;

public class BotConversationEngine : IBotConversationEngine
{
    private readonly ISessionManager _sessionManager;
    private readonly ITranxaService _tranxaService;
    private readonly ITranxaAuditLogRepository _auditRepo;
    private readonly IOtpService _otpService;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5);

    public BotConversationEngine(
        ISessionManager sessionManager,
        ITranxaService tranxaService,
        ITranxaAuditLogRepository auditRepo,
        IOtpService otpService)
    {
        _sessionManager = sessionManager;
        _tranxaService = tranxaService;
        _auditRepo = auditRepo;
        _otpService = otpService;
    }

    public async Task<string> ProcessMessageAsync(string userId, string message)
    {
        var ctx = await _sessionManager.GetSessionAsync(userId);
        message = message.Trim();

        if (DateTime.UtcNow - ctx.LastActivity > SessionTimeout &&
            ctx.Step != ConversationStep.Start)
        {
            ctx = new SessionContext();
            await _sessionManager.SaveSessionAsync(userId, ctx);
            return "⏳ <b>Sesión expirada</b>\nPor seguridad debes iniciar nuevamente.\n\nEscribe <b>hola</b>.";
        }

        ctx.LastActivity = DateTime.UtcNow;

        string response = ctx.Step switch
        {
            ConversationStep.Start => ShowWelcome(ctx),
            ConversationStep.MainMenu => HandleMainMenu(ctx),
            ConversationStep.WaitingForDocumentType => HandleDocType(ctx, message),
            ConversationStep.WaitingForDocumentNumber => await HandleDocNumber(ctx, message),
            ConversationStep.SelectProduct => HandleProductSelection(ctx, message),
            ConversationStep.ValidatingUser => await HandleOtpValidation(userId, ctx, message),
            ConversationStep.AuthenticatedMenu => await HandleAuthenticatedMenu(userId, ctx, message),
            _ => "Escribe <b>hola</b> para comenzar."
        };

        await _sessionManager.SaveSessionAsync(userId, ctx);
        return response;
    }

    private string ShowWelcome(SessionContext ctx)
    {
        ctx.Step = ConversationStep.MainMenu;
        return "🏦 <b>Banca Digital VIP</b>\n━━━━━━━━━━━━━━━━━━\nEscribe <b>hola</b> para iniciar.";
    }

    private string HandleMainMenu(SessionContext ctx)
    {
        ctx.Step = ConversationStep.WaitingForDocumentType;
        return "🪪 <b>Validación de identidad</b>\n1️⃣ Cédula\n2️⃣ Pasaporte";
    }

    private string HandleDocType(SessionContext ctx, string input)
    {
        ctx.DocumentType = input switch { "1" => "DNI", "2" => "PAS", _ => null };
        if (ctx.DocumentType == null) return "❌ Opción inválida.";
        ctx.Step = ConversationStep.WaitingForDocumentNumber;
        return "✍️ Ingresa tu número de documento";
    }

    private async Task<string> HandleDocNumber(SessionContext ctx, string docNumber)
    {
        ctx.DocumentNumber = docNumber;

        var response = await _tranxaService.GetCustomerProductsAsync(ctx.DocumentNumber, ctx.DocumentType!);

        if (response == null || response.Result != "Approved" || response.Cards == null || !response.Cards.Any())
        {
            ctx.Step = ConversationStep.Start;
            return "❌ No encontramos productos asociados.";
        }

        ctx.Cards = response.Cards;
        ctx.Step = ConversationStep.SelectProduct;

        return FormatProductsTable(ctx.Cards);
    }

    private string HandleProductSelection(SessionContext ctx, string input)
    {
        if (!int.TryParse(input, out int index) || index < 1 || index > ctx.Cards.Count)
            return "❌ Selección inválida.";

        var selected = ctx.Cards[index - 1];
        ctx.SelectedTokenId = selected.TokenId;

        ctx.Step = ConversationStep.ValidatingUser;
        _otpService.GenerateOtp(ctx.DocumentNumber!);

        return "🔐 Para continuar, ingresa el código OTP de 4 dígitos enviado por SMS.";
    }

    private Task<string> HandleOtpValidation(string userId, SessionContext ctx, string input)
    {
        if (!_otpService.ValidateOtp(ctx.DocumentNumber!, input))
            return Task.FromResult("❌ Código incorrecto. Intenta nuevamente.");

        ctx.Step = ConversationStep.AuthenticatedMenu;
        return Task.FromResult("✅ Identidad verificada correctamente.\n━━━━━━━━━━━━━━━━━━\n" + GetMenu());
    }

    private async Task<string> HandleAuthenticatedMenu(string userId, SessionContext ctx, string input)
    {
        var card = ctx.Cards.First(c => c.TokenId == ctx.SelectedTokenId);

        switch (input)
        {
            case "1":
                return $"💰 <b>Saldo disponible</b>\n━━━━━━━━━━━━━━━━━━\n{card.CurrBalance} {card.Currency}\n━━━━━━━━━━━━━━━━━━\n" + GetMenu();

            case "2":
                return FormatMovementsTable(card) + "\n━━━━━━━━━━━━━━━━━━\n" + GetMenu();

            case "3":
                var result = await _tranxaService.BlockCardAsync(card.TokenId, 1);
                _ = Task.Run(async () => { try { await _auditRepo.AddAsync(new TranxaAuditLog { UserChannelId = userId, Action = "BLOCK_CARD", TokenId = card.TokenId, Result = result?.Result ?? "ERROR", CreatedAt = DateTime.UtcNow }); } catch { } });

                return $"🔒 Resultado bloqueo: <b>{result?.Result}</b>\n━━━━━━━━━━━━━━━━━━\n" + GetMenu();

            case "4":
                ctx.Step = ConversationStep.SelectProduct;
                return FormatProductsTable(ctx.Cards);

            case "5":
                ctx.Step = ConversationStep.Start;
                return "👋 Sesión finalizada.\nEscribe <b>hola</b> para volver.";

            default:
                return GetMenu();
        }
    }

    private static string FormatProductsTable(List<CardDto> cards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("💼 <b>Tus productos</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━");
        sb.AppendLine(" # | Tarjeta              |        Saldo");
        sb.AppendLine("---------------------------------------------");

        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            sb.AppendLine($"{i + 1,2} | {MaskPan(c.Pan),-18} | {c.CurrBalance,12} {c.Currency}");
        }

        sb.AppendLine("━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("\nSelecciona el número del producto.");
        return sb.ToString();
    }

    private static string FormatMovementsTable(CardDto card)
    {
        if (card.Movements == null || !card.Movements.Any())
            return "📭 No hay movimientos recientes.";

        var sb = new StringBuilder();
        sb.AppendLine("📊 <b>Últimos movimientos</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━\\");
        sb.AppendLine("Fecha  | Descripción               |     Monto");
        sb.AppendLine("-----------------------------------------------");

        foreach (var m in card.Movements.Take(5))
        {
            var date = DateTime.Parse(m.MvDate).ToString("dd/MM").PadRight(6);
            var sign = m.MvSign == "Debit" ? "-" : "+";
            var amount = sign + m.MvAmt;
            var desc = m.MvDet.Length > 22 ? m.MvDet[..22] : m.MvDet;

            sb.AppendLine($"{date} | {desc.PadRight(24)} | {amount.PadLeft(10)}");
        }

        sb.AppendLine("━━━━━━━━━━━━━━━━━━\\");
        return sb.ToString();
    }

    private static string GetMenu() =>
        "¿Qué deseas hacer ahora?\n\n1️⃣ Consultar saldo\n2️⃣ Ver movimientos\n3️⃣ Bloquear tarjeta\n4️⃣ Cambiar producto\n5️⃣ Salir";

    private static string MaskPan(string pan) =>
        pan.Length > 4 ? $"**** **** **** {pan[^4..]}" : pan;
}
