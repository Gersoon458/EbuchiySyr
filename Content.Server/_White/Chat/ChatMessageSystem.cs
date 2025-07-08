using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Chat;

public sealed class ChatMessageSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChatMessageComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<ChatMessageComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnInteract(EntityUid uid, ChatMessageComponent component, InteractHandEvent args)
    {
        SendChatMessage(uid, component, args.User);
    }

    private void OnActivate(EntityUid uid, ChatMessageComponent component, ActivateInWorldEvent args)
    {
        SendChatMessage(uid, component, args.User);
    }

    private void SendChatMessage(EntityUid uid, ChatMessageComponent component, EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        // Проверяем кулдаун
        var currentTime = _timing.CurTime;
        if (currentTime - component.LastMessageTime < TimeSpan.FromSeconds(component.Cooldown))
            return;

        component.LastMessageTime = currentTime;

        // Форматируем отправителя с его цветом, шрифтом, размером И двоеточием
        var senderFormatted = component.SenderFont != null
            ? $"[font=\"{component.SenderFont}\" size={component.SenderFontSize}][color={component.SenderColor}]{component.Sender}:[/color][/font]"
            : $"[font size={component.SenderFontSize}][color={component.SenderColor}]{component.Sender}:[/color][/font]";

        // Форматируем сообщение с цветом, размером и типом шрифта
        var messageFormatted = component.MessageFont != null
            ? $"[font=\"{component.MessageFont}\" size={component.FontSize}][color={component.TextColor}]{component.Message}[/color][/font]"
            : $"[font size={component.FontSize}][color={component.TextColor}]{component.Message}[/color][/font]";

        // Объединяем без дополнительного двоеточия
        var wrappedMessage = $"{senderFormatted} {messageFormatted}";

        // Отправляем напрямую через ChatMessageToOne для сохранения форматирования
        _chatManager.ChatMessageToOne(
            ChatChannel.Server,
            messageFormatted,
            wrappedMessage,
            uid,
            false,
            actor.PlayerSession.Channel,
            recordReplay: true
        );

        // Воспроизводим звук только если он задан
        if (component.Sound != null)
        {
            _audioSystem.PlayEntity(component.Sound, actor.PlayerSession, uid);
        }
    }

    private bool TryParseHexColor(string hexColor, out Color color)
    {
        try
        {
            color = Color.FromHex(hexColor);
            return true;
        }
        catch
        {
            color = Color.White;
            return false;
        }
    }
}
