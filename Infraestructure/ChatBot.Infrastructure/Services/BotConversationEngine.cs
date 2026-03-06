using ChatBot.Application.DTOs.Tranza;
using ChatBot.Application.DTOs.Tranza.Models;
using ChatBot.Application.Interfaces.External;
using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Application.Interfaces.Services;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using ChatBot.Domain.ValueObjects;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatBot.Infrastructure.Services;

public class BotConversationEngine : IBotConversationEngine
{
    private readonly ISessionManager _sessionManager;
    private readonly ITranxaService _tranxaService;

    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IAuditEventRepository _auditRepository;

    private readonly IUnitOfWork _unitOfWork;

    private const int MAX_ATTEMPTS = 3;
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5);

    public BotConversationEngine(
        ISessionManager sessionManager,
        ITranxaService tranxaService,
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IAuditEventRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _sessionManager = sessionManager;
        _tranxaService = tranxaService;

        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _auditRepository = auditRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ProcessMessageAsync(string userId, string message)
    {
        var responses = new List<string>();

        Console.WriteLine($"[BOT] Incoming message | user={userId} | message={message}");

        //---------------------------------------
        // USER
        //---------------------------------------

        var user = await _userRepository.GetByWhatsAppAsync(userId);

        if (user == null)
        {
            user = new TranxaUser
            {
                WhatsAppNumber = new PhoneNumber(userId),
                FirstContactAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
        }

        //---------------------------------------
        // SESSION
        //---------------------------------------

        var session = await _sessionRepository.GetActiveSessionAsync(user.Id);

        if (session == null)
        {
            session = new TranxaSession
            {
                UserId = user.Id,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                Status = SessionStatus.Active
            };

            await _sessionRepository.AddAsync(session);
        }
        else
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
        }

        //---------------------------------------
        // SAVE MESSAGE IN
        //---------------------------------------

        await _messageRepository.AddAsync(
            new TranxaMessage
            {
                SessionId = session.Id,
                Direction = MessageDirection.In,
                MessageType = "Text",
                Content = message,
                CreatedAt = DateTime.UtcNow
            });

        await _unitOfWork.SaveChangesAsync();

        //---------------------------------------
        // SESSION CONTEXT
        //---------------------------------------

        var ctx = await _sessionManager.GetSessionAsync(userId);

        if (ctx == null)
        {
            ctx = new SessionContext
            {
                Step = ConversationStep.Start,
                LastActivity = DateTime.UtcNow
            };

            await _sessionManager.SaveSessionAsync(userId, ctx);
        }

        //---------------------------------------
        // SESSION TIMEOUT
        //---------------------------------------

        if (DateTime.UtcNow - ctx.LastActivity > SessionTimeout)
        {
            Console.WriteLine("[BOT] Session timeout");

            ctx.Step = ConversationStep.Start;
            ctx.StepAttempts = 0;
            ctx.OtpAttempts = 0;

            responses.Add("⏳ Tu sesión expiró por inactividad.\n\nEscribe *hola* para comenzar nuevamente.");

            await _sessionManager.SaveSessionAsync(userId, ctx);
            return responses;
        }

        ctx.LastActivity = DateTime.UtcNow;

        //---------------------------------------
        // LOOP DETECTION
        //---------------------------------------

        if (ctx.LastMessage == message)
        {
            ctx.LoopCount++;

            if (ctx.LoopCount >= 3)
            {
                ctx.Step = ConversationStep.Start;
                ctx.LoopCount = 0;

                responses.Add("🤖 Parece que estamos repitiendo el mismo mensaje.\n\nVolvamos a comenzar.\n\nEscribe *hola*.");
                return responses;
            }
        }
        else
        {
            ctx.LoopCount = 0;
        }

        ctx.LastMessage = message;

        //---------------------------------------
        // MAIN FLOW
        //---------------------------------------

        responses.Add(await HandleConversation(ctx, message, userId, user, session));

        await _sessionManager.SaveSessionAsync(userId, ctx);

        //---------------------------------------
        // SAVE MESSAGE OUT
        //---------------------------------------

        foreach (var response in responses)
        {
            await _messageRepository.AddAsync(
                new TranxaMessage
                {
                    SessionId = session.Id,
                    Direction = MessageDirection.Out,
                    MessageType = "Text",
                    Content = response,
                    CreatedAt = DateTime.UtcNow
                });
        }

        await _unitOfWork.SaveChangesAsync();

        return responses;
    }

    private async Task<string> HandleConversation(SessionContext ctx, string message, string userId, TranxaUser user, TranxaSession session)
    {
        return ctx.Step switch
        {
            ConversationStep.Start => ShowWelcome(ctx),

            ConversationStep.WaitingForDocumentType => HandleDocType(ctx, message),

            ConversationStep.WaitingForDocumentNumber => await HandleDocNumber(ctx, message),

            ConversationStep.ValidatingUser => await HandleOtpValidation(ctx, message),

            ConversationStep.SelectProduct => HandleProductSelection(ctx, message),

            ConversationStep.AuthenticatedMenu => await HandleAuthenticatedMenu(userId, ctx, message, user, session),

            _ => "Escribe hola para comenzar."
        };
    }

    //---------------------------------------
    // VALIDATION ENGINE
    //---------------------------------------

    private bool IsNumeric(string value) => value.All(char.IsDigit);

    private bool IsPassport(string value)
        => Regex.IsMatch(value, "^[A-Za-z0-9]+$");

    //---------------------------------------
    // STEPS
    //---------------------------------------

    private string ShowWelcome(SessionContext ctx)
    {
        ctx.Step = ConversationStep.WaitingForDocumentType;
        ctx.StepAttempts = 0;

        return "🏦 Banca Digital WOPA\n\nSelecciona tipo de documento:\n1️⃣ Cédula\n2️⃣ Pasaporte";
    }

    private string HandleDocType(SessionContext ctx, string input)
    {
        if (!IsNumeric(input))
            return "🙈 Ingresaste una letra.\n\nIngresa:\n1️⃣ Cédula\n2️⃣ Pasaporte";

        ctx.DocumentType = input switch
        {
            "1" => "CED",
            "2" => "PAS",
            _ => null
        };

        if (ctx.DocumentType == null)
            return "❌ La opción es inválida.\n\nSelecciona:\n1️⃣ Cédula\n2️⃣ Pasaporte";

        ctx.Step = ConversationStep.WaitingForDocumentNumber;

        return ctx.DocumentType == "CED"
            ? "✍️ Por favor, Ingresa tu número de cédula :"
            : "✍️ Por favor, Ingresa tu número de pasaporte :";
    }

    private async Task<string> HandleDocNumber(SessionContext ctx, string docNumber)
    {
        if (ctx.DocumentType == "CED" && !IsNumeric(docNumber))
            return "🙈 La cédula solo debe contener números.";

        if (ctx.DocumentType == "PAS" && !IsPassport(docNumber))
            return "🙈 El pasaporte solo puede contener letras y números.";

        ctx.DocumentNumber = docNumber;

        var response = await _tranxaService.GetCustomerProductsAsync(ctx.DocumentNumber, ctx.DocumentType!);

        if (response == null || response.Cards == null || !response.Cards.Any())
        {
            ctx.Step = ConversationStep.Start;
            return "❌ No encontramos productos asociados al documento ingresado.";
        }

        ctx.Cards = response.Cards;
        ctx.UserEmail = response.Person?.Email;

        var otp = await _tranxaService.GenerateOtpAsync(ctx.UserEmail!);

        if (otp == null || otp.OtpStatus != "Approved")
        {
            ctx.Step = ConversationStep.Start;
            return "❌ No fue posible generar el OTP.";
        }

        ctx.Step = ConversationStep.ValidatingUser;
        ctx.OtpAttempts = 0;

        return "🔐 Hemos enviado un código OTP a tu correo registrado. Por favor valida tu bandeja de entrada.\n\nCuando lo recibas ingresa el código:";
    }

    private async Task<string> HandleOtpValidation(SessionContext ctx, string otp)
    {
        if (!IsNumeric(otp))
            return "🙈 El OTP solo debe contener números.";

        var validation = await _tranxaService.ValidateOtpAsync(ctx.UserEmail!, otp);

        if (validation == null || validation.Result != "Approved")
        {
            ctx.OtpAttempts++;

            if (ctx.OtpAttempts >= MAX_ATTEMPTS)
            {
                ctx.OtpAttempts = 0;

                var newOtp = await _tranxaService.GenerateOtpAsync(ctx.UserEmail!);

                return "⚠️ Ingresaste incorrectamente el OTP 3 veces.\n\nTe enviamos un nuevo código.\n\nIngresa el nuevo OTP:";
            }

            return $"❌ OTP incorrecto.\nIntentos restantes: {MAX_ATTEMPTS - ctx.OtpAttempts}";
        }

        ctx.Step = ConversationStep.SelectProduct;

        return FormatProductsTable(ctx.Cards);
    }

    private string HandleProductSelection(SessionContext ctx, string input)
    {
        if (!IsNumeric(input))
            return "🙈 Ingresa el número del producto.";

        int index = int.Parse(input);

        if (index < 1 || index > ctx.Cards.Count)
            return $"❌ Selección inválida.\n\nIngresa un número entre 1 y {ctx.Cards.Count}.";

        ctx.SelectedTokenId = ctx.Cards[index - 1].TokenId;
        ctx.Step = ConversationStep.AuthenticatedMenu;

        return GetMenu();
    }

    private async Task<string> HandleAuthenticatedMenu(string userId, SessionContext ctx, string input, TranxaUser user, TranxaSession session)
    {
        if (!IsNumeric(input))
            return "🙈 Ingresa un número del menú.\n\n" + GetMenu();

        int option = int.Parse(input);

        var card = ctx.Cards.First(c => c.TokenId == ctx.SelectedTokenId);

        switch (option)
        {
            case 1:
                return $"💰 Saldo disponible\n{card.CurrBalance} {card.Currency}\n\n{GetMenu()}";

            case 2:
                return FormatMovementsTable(card) + "\n\n" + GetMenu();

            case 3:

                var result = await _tranxaService.BlockCardAsync(card.TokenId, 1);

                await _auditRepository.AddAsync(
                    new TranxaAuditEvent
                    {
                        SessionId = session.Id,
                        UserId = user.Id,
                        EventType = "BLOCK_CARD",
                        ExternalReference = card.TokenId,
                        Result = result?.Result ?? "ERROR",
                        Details = "Card block requested",
                        CreatedAt = DateTime.UtcNow
                    });

                await _unitOfWork.SaveChangesAsync();

                return $"🔒 Resultado bloqueo: {result?.Result}\n\n" + GetMenu();

            case 4:
                ctx.Step = ConversationStep.SelectProduct;
                return FormatProductsTable(ctx.Cards);

            case 5:
                ctx.Step = ConversationStep.Start;
                return "👋 Sesión finalizada.\n\nEscribe *hola* para iniciar nuevamente.";

            default:
                return "❌ Opción inválida.\n\n" + GetMenu();
        }
    }

    //---------------------------------------
    // FORMATTERS
    //---------------------------------------

    private static string FormatProductsTable(List<CardDto> cards)
    {
        var sb = new StringBuilder();

        sb.AppendLine("💼 Tus productos disponibles:\n");

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

        sb.AppendLine("📊 *Últimos movimientos*\n");

        foreach (var m in card.Movements.Take(5))
        {
            var date = DateTime.Parse(m.MvDate).ToString("dd/MM");

            decimal amount = decimal.Parse(m.MvAmt);

            var formattedAmount = amount >= 0
                ? $"🟢 +${amount:N2}"
                : $"🔴 -${Math.Abs(amount):N2}";

            sb.AppendLine($"📅 {date}");
            sb.AppendLine($"💳 {m.MvDet}");
            sb.AppendLine($"💰 {formattedAmount}");
            sb.AppendLine("\n──────────────\n");
        }

        return sb.ToString();
    }

    private static string GetMenu()
        => "¿Qué deseas hacer ahora?\n1️⃣ Consultar saldo\n2️⃣ Ver movimientos\n3️⃣ Bloquear tarjeta\n4️⃣ Cambiar producto\n5️⃣ Salir";
}