using Robust.Shared.Serialization;

namespace Content.Shared.Tutorial;

[Serializable, NetSerializable]
public sealed class RequestTutorialEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class TutorialResponseEvent : EntityEventArgs
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
