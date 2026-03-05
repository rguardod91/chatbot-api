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

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5);

    public BotConversationEngine(
        ISessionManager sessionManager,
        ITranxaService tranxaService,
        ITranxaAuditLogRepository auditRepo)
    {
        _sessionManager = sessionManager;
        _tranxaService = tranxaService;
        _auditRepo = auditRepo;
    }

    public async Task<string> ProcessMessageAsync(string userId, string message)
    {
        Console.WriteLine($"[BOT] Incoming message | user={userId} | message={message}");

        var ctx = await _sessionManager.GetSessionAsync(userId);

        if (ctx == null)
        {
            Console.WriteLine($"[BOT] No session found. Creating new session for user={userId}");

            ctx = new SessionContext
            {
                Step = ConversationStep.Start,
                LastActivity = DateTime.UtcNow
            };

            await _sessionManager.SaveSessionAsync(userId, ctx);
        }

        message = message?.Trim() ?? string.Empty;

        if (ctx.Step != ConversationStep.Start &&
            DateTime.UtcNow.Subtract(ctx.LastActivity) > SessionTimeout)
        {
            Console.WriteLine($"[BOT] Session expired | user={userId}");

            ctx = new SessionContext
            {
                Step = ConversationStep.Start,
                LastActivity = DateTime.UtcNow
            };

            await _sessionManager.SaveSessionAsync(userId, ctx);

            return "⏳ Tu sesión ha expirado por inactividad (5 minutos).\n\nEscribe *hola* para iniciar nuevamente.";
        }

        ctx.LastActivity = DateTime.UtcNow;

        Console.WriteLine($"[BOT] Current step={ctx.Step}");

        string response = ctx.Step switch
        {
            ConversationStep.Start => ShowWelcome(ctx),
            ConversationStep.WaitingForDocumentType => HandleDocType(ctx, message),
            ConversationStep.WaitingForDocumentNumber => await HandleDocNumber(ctx, message),
            ConversationStep.ValidatingUser => await HandleOtpValidation(ctx, message),
            ConversationStep.SelectProduct => HandleProductSelection(ctx, message),
            ConversationStep.AuthenticatedMenu => await HandleAuthenticatedMenu(userId, ctx, message),
            _ => "Escribe hola para comenzar."
        };

        await _sessionManager.SaveSessionAsync(userId, ctx);

        Console.WriteLine($"[BOT] Response generated | user={userId}");

        return response;
    }

    private string ShowWelcome(SessionContext ctx)
    {
        Console.WriteLine("[BOT] Step=Start → WaitingForDocumentType");

        ctx.Step = ConversationStep.WaitingForDocumentType;

        return "🏦 Banca Digital WOPA\nSelecciona tipo de documento:\n1️⃣ Cédula\n2️⃣ Pasaporte";
    }

    private string HandleDocType(SessionContext ctx, string input)
    {
        Console.WriteLine($"[BOT] HandleDocType input={input}");

        ctx.DocumentType = input switch { "1" => "DNI", "2" => "PAS", _ => null };

        if (ctx.DocumentType == null)
        {
            Console.WriteLine("[BOT] Invalid document type selected");
            return "❌ Opción inválida.";
        }

        ctx.Step = ConversationStep.WaitingForDocumentNumber;

        Console.WriteLine($"[BOT] DocumentType selected={ctx.DocumentType}");

        return "✍️ Ingresa tu número de documento:";
    }

    private async Task<string> HandleDocNumber(SessionContext ctx, string docNumber)
    {
        Console.WriteLine($"[BOT] Requesting products | doc={docNumber}");

        ctx.DocumentNumber = docNumber;

        var response = await _tranxaService.GetCustomerProductsAsync(ctx.DocumentNumber, ctx.DocumentType!);

        if (response == null || response.Result != "Approved" || response.Cards == null || !response.Cards.Any())
        {
            Console.WriteLine("[BOT] No products found for user");

            ctx.Step = ConversationStep.Start;

            return "❌ No encontramos productos asociados.";
        }

        Console.WriteLine($"[BOT] Products found count={response.Cards.Count}");

        ctx.Cards = response.Cards;
        ctx.UserEmail = response.Person?.Email;

        if (string.IsNullOrWhiteSpace(ctx.UserEmail))
        {
            Console.WriteLine("[BOT] Email not found in response");

            ctx.Step = ConversationStep.Start;

            return "❌ No se pudo obtener el correo del cliente.";
        }

        Console.WriteLine($"[BOT] Generating OTP for email={ctx.UserEmail}");

        var otp = await _tranxaService.GenerateOtpAsync(ctx.UserEmail);

        if (otp == null || otp.OtpStatus != "Approved")
        {
            Console.WriteLine("[BOT] OTP generation failed");

            ctx.Step = ConversationStep.Start;

            return "❌ No fue posible generar el OTP.";
        }

        ctx.Step = ConversationStep.ValidatingUser;

        Console.WriteLine("[BOT] OTP generated successfully");

        return "🔐 Te enviamos un OTP a tu correo registrado. Ingresa el código para continuar.";
    }

    private async Task<string> HandleOtpValidation(SessionContext ctx, string otp)
    {
        Console.WriteLine($"[BOT] Validating OTP | email={ctx.UserEmail}");

        var validation = await _tranxaService.ValidateOtpAsync(ctx.UserEmail!, otp);

        if (validation == null || validation.Result != "Approved")
        {
            Console.WriteLine("[BOT] OTP validation failed");
            return $"❌ {validation?.DeclineReason ?? "OTP inválido"}";
        }

        Console.WriteLine("[BOT] OTP validated successfully");

        ctx.Step = ConversationStep.SelectProduct;

        return FormatProductsTable(ctx.Cards);
    }

    private string HandleProductSelection(SessionContext ctx, string input)
    {
        Console.WriteLine($"[BOT] Product selection input={input}");

        if (!int.TryParse(input, out int index) || index < 1 || index > ctx.Cards.Count)
        {
            Console.WriteLine("[BOT] Invalid product selection");
            return "❌ Selección inválida.";
        }

        ctx.SelectedTokenId = ctx.Cards[index - 1].TokenId;

        Console.WriteLine($"[BOT] Product selected tokenId={ctx.SelectedTokenId}");

        ctx.Step = ConversationStep.AuthenticatedMenu;

        return GetMenu();
    }

    private async Task<string> HandleAuthenticatedMenu(string userId, SessionContext ctx, string input)
    {
        Console.WriteLine($"[BOT] Authenticated menu option={input}");

        var card = ctx.Cards.First(c => c.TokenId == ctx.SelectedTokenId);

        switch (input)
        {
            case "1":
                Console.WriteLine("[BOT] Balance inquiry");

                return $"💰 Saldo disponible\n{card.CurrBalance} {card.Currency}\n\n" + GetMenu();

            case "2":
                Console.WriteLine("[BOT] Movement inquiry");

                return FormatMovementsTable(card) + "\n\n" + GetMenu();

            case "3":

                Console.WriteLine($"[BOT] Blocking card tokenId={card.TokenId}");

                var result = await _tranxaService.BlockCardAsync(card.TokenId, 1);

                await _auditRepo.AddAsync(new TranxaAuditLog
                {
                    UserChannelId = userId,
                    Action = "BLOCK_CARD",
                    TokenId = card.TokenId,
                    Result = result?.Result ?? "ERROR",
                    CreatedAt = DateTime.UtcNow
                });

                Console.WriteLine($"[BOT] Block result={result?.Result}");

                return $"🔒 Resultado bloqueo: {result?.Result}\n\n" + GetMenu();

            case "4":

                Console.WriteLine("[BOT] Changing product");

                ctx.Step = ConversationStep.SelectProduct;

                return FormatProductsTable(ctx.Cards);

            case "5":

                Console.WriteLine("[BOT] Ending session");

                ctx.Step = ConversationStep.Start;

                return "👋 Sesión finalizada. Escribe hola para iniciar nuevamente.";

            default:

                Console.WriteLine("[BOT] Invalid menu option");

                return GetMenu();
        }
    }

    private static string FormatProductsTable(List<CardDto> cards)
    {
        var sb = new StringBuilder();

        sb.AppendLine("💼 Tus productos disponibles:");

        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];

            sb.AppendLine($"{i + 1}️⃣ ****{c.Pan[^4..]} | {c.CurrBalance} {c.Currency}");
        }

        sb.AppendLine("\nSelecciona el número del producto:");

        return sb.ToString();
    }

    private static string FormatMovementsTable(CardDto card)
    {
        if (card.Movements == null || !card.Movements.Any())
            return "📭 No hay movimientos recientes.";

        var sb = new StringBuilder();

        sb.AppendLine("📊 Últimos movimientos\n");

        sb.AppendLine("────────────────────────────────────────────────────");
        sb.AppendLine("Fecha   Descripción                 Monto");
        sb.AppendLine("──────  ──────────────────────────  ───────");

        foreach (var m in card.Movements.Take(5))
        {
            var date = DateTime.Parse(m.MvDate).ToString("dd/MM");

            var description = m.MvDet.Length > 25
                ? m.MvDet.Substring(0, 25)
                : m.MvDet.PadRight(25);

            decimal amount = decimal.Parse(m.MvAmt);

            var formattedAmount = amount >= 0
                ? $"+${amount:N2}"
                : $"-${Math.Abs(amount):N2}";

            sb.AppendLine($"{date.PadRight(6)}  {description}  {formattedAmount.PadLeft(8)}");
        }

        sb.AppendLine("────────────────────────────────────────────────────");

        return sb.ToString();
    }

    private static string GetMenu() =>
        "¿Qué deseas hacer ahora?\n1️⃣ Consultar saldo\n2️⃣ Ver movimientos\n3️⃣ Bloquear tarjeta\n4️⃣ Cambiar producto\n5️⃣ Salir";
}