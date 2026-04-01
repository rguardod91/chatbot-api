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

        var sessionTimeoutMinutes = 5;

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

        return "🏦 Bienvenido a tu Banca Digital\n\nSelecciona tipo de documento:\n1️⃣ Cédula\n2️⃣ Pasaporte";
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

        return "🔐 Hemos enviado un Código unico de validación al correo registrado. Por favor valida tu bandeja de entrada\n\nCuando lo recibas, por favor ingresa el código:";
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
        if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input.Trim(), out int index))
            return "🙈 Ingresa el número del producto sobre el cual deseas realizar operaciones.";

        if (index < 1 || index > ctx.Cards.Count)
            return $"❌ Selección inválida.\n\nIngresa un número entre 1 y {ctx.Cards.Count}.";

        var selectedCard = ctx.Cards[index - 1];
        ctx.SelectedTokenId = selectedCard.TokenId;
        ctx.Step = ConversationStep.AuthenticatedMenu;

        return GetMenu(selectedCard);
    }

    private async Task<string> HandleAuthenticatedMenu(string userId, SessionContext ctx, string input, TranxaUser user, TranxaSession session)
    {
        var card = ctx.Cards.First(c => c.TokenId == ctx.SelectedTokenId);

        if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input.Trim(), out int option))
            return "🙈 Ingresa un número del menú.\n\n" + GetMenu(card);

        if (option < 1 || option > 5)
            return "❌ Opción inválida. Por favor ingresa un número entre 1 y 5.\n\n" + GetMenu(card);

        switch (option)
        {
            case 1:
                return $"💰 Saldo disponible\n{card.CurrBalance} {card.Currency}\n\n{GetMenu(card)}";

            case 2:
                Console.WriteLine("[BOT] Consultando movimientos...");

                return "⏳ Consultando últimos movimientos...\n\n"
                       + FormatMovementsTable(card)
                       + "\n\n"
                       + GetMenu(card);

            case 3:
                // Status 0 significa activo. Diferente de 0 significa inactivo/bloqueado.
                bool isCurrentlyActive = card.CardStatus == 0;

                // Si está activo, enviamos 1 para desactivar temporalmente. Si está inactivo, enviamos 0 para activar.
                int statusToSend = isCurrentlyActive ? 9 : 0;
                string actionResultText = isCurrentlyActive ? "Desactivado temporalmente" : "Activado exitosamente";
                string eventType = isCurrentlyActive ? "DEACTIVATE_CARD" : "ACTIVATE_CARD";

                Console.WriteLine($"[BOT] Iniciando {(isCurrentlyActive ? "desactivación" : "activación")} tarjeta {card.TokenId}");

                var result = await _tranxaService.BlockCardAsync(card.TokenId, statusToSend);

                var estado = TranslateResult(result?.Result);
                var approvalCode = result?.ApprovalCode ?? "N/A";

                await _auditRepository.AddAsync(
                    new TranxaAuditEvent
                    {
                        SessionId = session.Id,
                        UserId = user.Id,
                        EventType = eventType,
                        ExternalReference = card.TokenId,
                        Result = result?.Result ?? "ERROR",
                        Details = $"ApprovalCode={approvalCode}",
                        CreatedAt = DateTime.UtcNow
                    });

                await _unitOfWork.SaveChangesAsync();

                // Actualizamos el estado en memoria para que el menú refleje el cambio inmediatamente
                if (result?.Result == "Approved")
                {
                    card.CardStatus = isCurrentlyActive ? 9 : 0;
                }

                Console.WriteLine($"[BOT] Resultado={estado} ApprovalCode={approvalCode}");

                return $"⚙️ El producto ****{card.Pan[^4..]} ha sido {actionResultText}.\n\nEstado: {estado}\nCódigo de aprobación: {approvalCode}\n\n{GetMenu(card)}";

            case 4:
                ctx.Step = ConversationStep.SelectProduct;
                return FormatProductsTable(ctx.Cards);

            case 5:
                ctx.Step = ConversationStep.Start;
                return "👋 Sesión finalizada.\n\nEscribe *hola* para iniciar nuevamente.";

            default:
                return "❌ Opción inválida.\n\n" + GetMenu(card);
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

    private static string GetMenu(CardDto card)
    {
        // Si el status es diferente de 0, mostramos la opción de activar.
        string activationMenuText = card.CardStatus == 0
            ? "3️⃣ Desactivación temporal"
            : "3️⃣ Activar tarjeta";

        return $"¿Deseas realizar algo más?\n1️⃣ Consultar saldo\n2️⃣ Ver movimientos\n{activationMenuText}\n4️⃣ Cambiar producto\n5️⃣ Salir";
    }
}
