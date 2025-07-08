using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Chat;

[RegisterComponent]
public sealed partial class ChatMessageComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("message")]
    public string Message = "Добро пожаловать!";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sender")]
    public string Sender = "Система";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("textColor")]
    public string TextColor = "#00FF00";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("senderColor")]
    public string SenderColor = "#FFFFFF";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("senderFont")]
    public string? SenderFont = null;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("senderFontSize")]
    public int SenderFontSize = 14;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("messageFont")]
    public string? MessageFont = null;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fontSize")]
    public int FontSize = 14;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sound")]
    public SoundSpecifier? Sound = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastMessageTime = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cooldown")]
    public float Cooldown = 0.1f;
}
