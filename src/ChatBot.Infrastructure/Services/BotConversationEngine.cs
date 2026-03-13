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

        //---------------------------------------
        // SESSION
        //---------------------------------------

        var session = await _sessionRepository.GetActiveSessionAsync(user.Id, 5);

        if (session == null)
        {
            Console.WriteLine($"[BOT] No active session found for user {user.Id}");

            //---------------------------------------
            // DOUBLE CHECK (ANTI RACE CONDITION)
            //---------------------------------------

            session = await _sessionRepository.GetActiveSessionAsync(user.Id, 5);

            if (session == null)
            {
                Console.WriteLine($"[BOT] Creating new session for user {user.Id}");

                session = new TranxaSession
                {
                    UserId = user.Id,
                    StartedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    Status = SessionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await _sessionRepository.AddAsync(session);

                await _unitOfWork.SaveChangesAsync();
            }
        }
        else
        {
            session.LastActivityAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(session);

            await _unitOfWork.SaveChangesAsync();
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
        // SESSION TIMEOUT (CONTROLADO POR DB)
        //---------------------------------------

        var sessionTimeoutMinutes = 1;

        if (session.LastActivityAt < DateTime.UtcNow.AddMinutes(-sessionTimeoutMinutes))
        {
            Console.WriteLine($"[BOT] Session expired | sessionId={session.Id}");

            //---------------------------------------
            // UPDATE SESSION STATUS IN DB
            //---------------------------------------

            session.Status = SessionStatus.Expired;
            session.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(session);

            await _unitOfWork.SaveChangesAsync();

            //---------------------------------------
            // RESET CONTEXT
            //---------------------------------------

            ctx.ResetSession();

            responses.Add("⏳ Tu sesión expiró por inactividad.\n\nEscribe *hola* para comenzar nuevamente.");

            await _sessionManager.SaveSessionAsync(userId, ctx);

            return responses;
        }

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

    private static bool IsNumeric(string value)
      => Regex.IsMatch(value, @"^[^\s]+$");

    private static bool IsPassport(string value)
        => Regex.IsMatch(value, @"^[^\s]+$");

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

        Console.WriteLine($"[BOT] Consultando productos cliente {docNumber}");

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
            return "❌ No fue posible generar el Código de Validación.";
        }

        ctx.Step = ConversationStep.ValidatingUser;
        ctx.OtpAttempts = 0;

        return "🔐 Hemos enviado un Código de Validación a tu correo registrado.\n\nCuando lo recibas ingresa el código:";
    }

    private async Task<string> HandleOtpValidation(SessionContext ctx, string otp)
    {
        if (!IsNumeric(otp))
            return "🙈 El Código de Validación solo debe contener números.";

        var validation = await _tranxaService.ValidateOtpAsync(ctx.UserEmail!, otp);

        if (validation == null || validation.Result != "Approved")
        {
            ctx.OtpAttempts++;

            if (ctx.OtpAttempts >= MAX_ATTEMPTS)
            {
                ctx.OtpAttempts = 0;
                await _tranxaService.GenerateOtpAsync(ctx.UserEmail!);

                return "⚠️ Ingresaste incorrectamente el Código de Validación 3 veces.\n\nTe enviamos un nuevo código.\n\nIngresa el nuevo Código de Validación:";
            }

            return $"❌ Código de Validación incorrecto.\nIntentos restantes: {MAX_ATTEMPTS - ctx.OtpAttempts}";
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

                Console.WriteLine("[BOT] Consultando movimientos...");

                return "⏳ Consultando últimos movimientos...\n\n"
                       + FormatMovementsTable(card)
                       + "\n\n"
                       + GetMenu();

            case 3:

                Console.WriteLine($"[BOT] Iniciando bloqueo tarjeta {card.TokenId}");

                var result = await _tranxaService.BlockCardAsync(card.TokenId, 2);

                var estado = TranslateResult(result?.Result);
                var approvalCode = result?.ApprovalCode ?? "N/A";

                await _auditRepository.AddAsync(
                    new TranxaAuditEvent
                    {
                        SessionId = session.Id,
                        UserId = user.Id,
                        EventType = "BLOCK_CARD",
                        ExternalReference = card.TokenId,
                        Result = result?.Result ?? "ERROR",
                        Details = $"ApprovalCode={approvalCode}",
                        CreatedAt = DateTime.UtcNow
                    });

                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine($"[BOT] Resultado bloqueo={estado} ApprovalCode={approvalCode}");

                return $"🔒 Resultado del bloqueo\n\nEstado: {estado}\nCódigo de aprobación: {approvalCode}\n\n{GetMenu()}";

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
    // HELPERS
    //---------------------------------------

    private static string TranslateResult(string? result)
    {
        return result switch
        {
            "Approved" => "Aprobado",
            "Declined" => "Rechazado",
            _ => result ?? "Desconocido"
        };
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