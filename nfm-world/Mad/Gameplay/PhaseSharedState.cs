using System.Diagnostics.CodeAnalysis;
using NFMWorldLibrary.Backend;

namespace NFMWorld.Gameplay;

public class PhaseSharedState
{
    public static BackendStage? CurrentStage;
    public static ClientStageRenderer? ClientStageRenderer;

    [MemberNotNull(nameof(CurrentStage))]
    [MemberNotNull(nameof(ClientStageRenderer))]
    public static void SetStage(BackendStage stage, ClientStageRenderer? renderer = null)
    {
        CurrentStage = stage;
        ClientStageRenderer = renderer ?? new ClientStageRenderer(GameSparker.GraphicsDevice, stage);
    }
}