using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace TheWitch.TheWitchCode.Config;

/// <summary>Broadcasts the host's TurboWitchery setting so all clients play by the same rule.</summary>
public sealed class TurboWitcheryMessage : ICustomMessage
{
    public bool Turbo;

    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer) => writer.WriteBool(Turbo);

    public void Deserialize(PacketReader reader) => Turbo = reader.ReadBool();

    public void HandleMessage(ulong senderId) => WitchConfig.SyncedTurbo = Turbo;
}
