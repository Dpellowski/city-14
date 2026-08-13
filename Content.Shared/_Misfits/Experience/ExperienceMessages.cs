using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Experience;

/// <summary>
/// Requests the experience snapshot for the sender's active in-round character,
/// falling back to their selected character when they have not spawned.
/// No target identifier is accepted from the client.
/// </summary>
public sealed class MsgCharacterExperienceRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
    }
}

/// <summary>
/// Sends all experience tracks for the receiver's current character.
/// </summary>
public sealed class MsgCharacterExperienceUpdate : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public bool HasCharacter;
    public string CharacterName = string.Empty;
    public Dictionary<ExperienceGroup, long> Experience = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        HasCharacter = buffer.ReadBoolean();
        CharacterName = buffer.ReadString();
        var count = buffer.ReadByte();
        Experience.Clear();
        Experience.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            var group = (ExperienceGroup) buffer.ReadByte();
            var value = buffer.ReadInt64();
            Experience[group] = value;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(HasCharacter);
        buffer.Write(CharacterName);
        buffer.Write((byte) Experience.Count);

        foreach (var (group, value) in Experience)
        {
            buffer.Write((byte) group);
            buffer.Write(value);
        }
    }
}
