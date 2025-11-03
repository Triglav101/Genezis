using Robust.Shared.Serialization;

namespace Content.Shared._Genezis.Drug;

[Serializable, NetSerializable]
public enum DrugInitializeMachineVisualState
{
    Idle,
    Running
}

public enum DrugInitializeMachineVisualizerLayers : byte
{
    Base,
    BaseUnlit
}

