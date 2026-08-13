using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.CivicPoints;

/// <summary>
/// Public priority printed on an ID. The private point balance and permanent profile ID are never networked.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CivicPriorityIdCardComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public CivicPriority Priority = CivicPriority.Priority7;
}
