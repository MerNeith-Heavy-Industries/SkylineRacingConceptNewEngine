using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;

namespace NFMWorld;

public class Lighting
{
    public IReadOnlyList<Camera> LightCameras;
    public IReadOnlyList<RenderTarget2D?> ShadowMaps;

    /// <summary>
    /// Describes the current render pass (shadow cascade or main colour pass).
    /// Replaces the boolean <see cref="IsCreateShadowMap"/> plus cascade-index pattern.
    /// </summary>
    public RenderPass RenderPass { get; }

    [MemberNotNullWhen(true, nameof(CascadeLightCamera))]
    public bool IsCreateShadowMap => RenderPass.IsShadow;

    public int NumCascade => RenderPass.CascadeIndex;

    public int TotalCascades => RenderPass.TotalCascades;

    /// <summary>
    /// New-style constructor using <see cref="RenderPass"/>.
    /// </summary>
    public Lighting(
        IReadOnlyList<Camera> lightCameras,
        IReadOnlyList<RenderTarget2D?> shadowMaps,
        RenderPass renderPass
    )
    {
        LightCameras = lightCameras;
        ShadowMaps = shadowMaps;
        RenderPass = renderPass;

        if (renderPass.IsShadow && renderPass.CascadeIndex >= 0)
        {
            CascadeLightCamera = LightCameras[renderPass.CascadeIndex];
        }
    }

    /// <summary>
    /// Legacy constructor. Prefer the <see cref="RenderPass"/>-based overload.
    /// </summary>
    public Lighting(
        IReadOnlyList<Camera> lightCameras,
        RenderTarget2D?[] shadowMaps,
        bool isCreateShadowMap = false,
        int numCascade = -1,
        int totalCascades = 3
    )
        : this(
            lightCameras,
            shadowMaps,
            isCreateShadowMap
                ? NFMWorld.RenderPass.Shadow(numCascade, totalCascades)
                : NFMWorld.RenderPass.Main(totalCascades))
    {
    }

    public Camera? CascadeLightCamera;

    public void SetShadowMapParameters(Effect effect)
    {
        if (LightCameras.Count > 0)
        {
            effect.Parameters["LightViewProj0"]?.SetValue(LightCameras[0].ViewProjectionMatrix);
        }

        if (LightCameras.Count > 1)
        {
            effect.Parameters["LightViewProj1"]?.SetValue(LightCameras[1].ViewProjectionMatrix);
        }

        if (LightCameras.Count > 2)
        {
            effect.Parameters["LightViewProj2"]?.SetValue(LightCameras[2].ViewProjectionMatrix);
        }

        if (!IsCreateShadowMap)
        {
            if (TotalCascades > 0)
            {
                effect.Parameters["ShadowMap0"]?.SetValue(ShadowMaps[0]);

                if (TotalCascades > 1)
                {
                    effect.Parameters["ShadowMap1"]?.SetValue(ShadowMaps[1]);
                    
                    if (TotalCascades > 2)
                    {
                        effect.Parameters["ShadowMap2"]?.SetValue(ShadowMaps[2]);
                    }
                    else
                    {
                        effect.Parameters["ShadowMap2"]?.SetValue((Texture?)null);
                    }
                }
                else
                {
                    effect.Parameters["ShadowMap1"]?.SetValue((Texture?)null);
                    effect.Parameters["ShadowMap2"]?.SetValue((Texture?)null);
                }
            }
            else
            {
                effect.Parameters["ShadowMap0"]?.SetValue((Texture?)null);
                effect.Parameters["ShadowMap1"]?.SetValue((Texture?)null);
                effect.Parameters["ShadowMap2"]?.SetValue((Texture?)null);
            }
        }
        
        effect.Parameters["NumCascades"]?.SetValue(TotalCascades);

        effect.Parameters["LightDirection"]?.SetValue(World.LightDirection);
    }
}