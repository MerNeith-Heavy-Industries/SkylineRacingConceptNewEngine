using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Accounts;
using NFMWorld.DriverInterface;
using NFMWorld.Gameplay;
using NFMWorld.Sfx;
using NFMWorld.UI;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Rad;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga.Events;
using Path = System.IO.Path;

namespace NFMWorld;

public static class GameSparker
{
    public static WorldGame Game = null!;
    public static GraphicsDevice GraphicsDevice = null!;
    public static readonly string version = GetVersionString();
    public static AccountManager AccountManager = new AccountManager();

    private static string GetVersionString()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
        if (attributes.Length > 0 && attributes[0] is AssemblyInformationalVersionAttribute infoVersion)
        {
            var version = infoVersion.InformationalVersion;
            // clip the commit hash
            var parts = version.Split('-');
            if (parts.Length >= 3)
            {
                var hash = parts[^1];
                if (hash.Length > 8)
                {
                    parts[^1] = hash.Substring(0, 8);
                    return string.Join("-", parts);
                }
            }
            return version;
        }
        return "NFM-World dev";
    }

    public static BasePhase CurrentPhase
    {
        get;
        private set;
    } = null!;

    public static void SetPhase(BasePhase phase)
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        CurrentPhase?.Exit();
        CurrentPhase = phase;
        CurrentPhase.Enter();
    }

    public static IRadicalMusic? CurrentMusic
    {
        get;
        set
        {
            field?.SetPaused(true);

            field = value;

            if (field != null)
            {
                field.SetVolume(IRadicalMusic.CurrentVolume);
                field.Play();
            }
        }
    }
    /// <summary>
    /// Use remastered music (soundtrackremaster in stage files) where available.
    /// </summary>
    public static bool UseRemasteredMusic = false;

    public static MainMenuPhase MainMenu = null!;
    public static MessageWindow MessageWindow = new();
    public static ModelEditorPhase ModelEditor = null!;
    public static StageEditorPhase StageEditor = null!;

    public static Dictionary<Rad3d, Mesh> stage_part_meshes = new(Rad3d.VisualEqualityComparer.Instance);
    public static Mesh error_mesh = null!;
    
    public static bool devRenderTrackers = false;
    
    public static DevConsole devConsole = new();

    public static SettingsMenu SettingsMenu;

    /////////////////////////////////

    public static Dictionary<Key, bool> DebugKeyStates = new();

    public static void KeyPressed(Key key)
    {
        DebugKeyStates[key] = true;

        var bindings = SettingsMenu.Bindings;
        
        if (key == bindings.ToggleDevConsole)
        {
            devConsole.Toggle();
        }
    }

    public static void KeyReleased(Key key)
    {
        DebugKeyStates[key] = false;
        
    }

    public static List<string> GetAvailableStages()
    {
        var stages = new List<string>();
        var stagesPath = "data/stages";
        
        if (Directory.Exists(stagesPath))
        {
            // recursive search
            foreach (var file in Directory.GetFiles(stagesPath, "*.txt", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(stagesPath, file);
                var pathWithoutExtension = Path.ChangeExtension(relativePath, null);
                stages.Add(pathWithoutExtension.Replace('\\', '/'));
            }
        }
        
        // sort numbers properly
        stages.Sort((a, b) => {
            var aSegments = a.Split('/');
            var bSegments = b.Split('/');
            
            for (int seg = 0; seg < Math.Min(aSegments.Length, bSegments.Length); seg++)
            {
                var aParts = System.Text.RegularExpressions.Regex.Split(aSegments[seg], @"(\d+)")
                    .Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var bParts = System.Text.RegularExpressions.Regex.Split(bSegments[seg], @"(\d+)")
                    .Where(s => !string.IsNullOrEmpty(s)).ToArray();
                
                for (int i = 0; i < Math.Min(aParts.Length, bParts.Length); i++)
                {
                    if (int.TryParse(aParts[i], out var aNum) && int.TryParse(bParts[i], out var bNum))
                    {
                        int numCompare = aNum.CompareTo(bNum);
                        if (numCompare != 0) return numCompare;
                    }
                    else
                    {
                        int strCompare = string.Compare(aParts[i], bParts[i], StringComparison.OrdinalIgnoreCase);
                        if (strCompare != 0) return strCompare;
                    }
                }
                
                if (aParts.Length != bParts.Length)
                    return aParts.Length.CompareTo(bParts.Length);
            }
            
            return aSegments.Length.CompareTo(bSegments.Length);
        });
        
        return stages;
    }
    public static void Load(WorldGame game)
    {
        Game = game;
        GraphicsDevice = game.GraphicsDevice;

        foreach (var stageParts in (Span<UnlimitedArray<Rad3d>>)[BackendGameSparker.stage_parts, BackendGameSparker.vendor_stage_parts, BackendGameSparker.user_stage_parts])
        foreach (var stagePart in stageParts)
        {
            try
            {
                var mesh = new Mesh(GraphicsDevice, stagePart);
                stage_part_meshes[stagePart] = mesh;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Logging.Debug($"Error creating mesh for stage part '{stagePart.FileName}': {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        error_mesh = new Mesh(GraphicsDevice, BackendGameSparker.error_mesh);

        SfxLibrary.LoadSounds();

        // init menu
        SettingsMenu = new SettingsMenu(game);
        MainMenu = new MainMenuPhase(GraphicsDevice);

        PhaseSharedState.SelectedStageName = "nfm2/16_4dv";
        MainMenu.LoadStage(PhaseSharedState.SelectedStageName);
        
        SetPhase(MainMenu);

        // Initialize ModelEditor after cars are loaded
        ModelEditor = new ModelEditorPhase(GraphicsDevice);
        
        StageEditor = new StageEditorPhase(GraphicsDevice);
    }
    
    public static Mesh GetStagePartMesh(Rad3d stagePart)
    {
        ref var mesh = ref CollectionsMarshal.GetValueRefOrAddDefault(stage_part_meshes, stagePart, out var exists);
        if (exists)
        {
            return mesh!;
        }

        return mesh = new Mesh(GraphicsDevice, stagePart);
    }

    public static void StartModelViewer()
    {
        SetPhase(ModelEditor);
    }
    
    public static void ExitEditor()
    {
        SetPhase(MainMenu);
        devRenderTrackers = false;
    }

    public static void StartStageEditor()
    {
        SetPhase(StageEditor);
    }
    
    public static void ReturnToMainMenu()
    {
        SetPhase(MainMenu);
    }

    public static void GameTick()
    {
        World.GameTick();
        FrameTrace.ClearMessages();
    }

    public static void Render()
    {
    }

    public static void Render3DOverlays()
    {
        CurrentPhase.Render3DOverlays();
    }

    public static void RenderImgui()
    {
        devConsole.Render();
        MessageWindow.Render();
        SettingsMenu.Render();
        CurrentPhase.RenderImgui();
    }

    public static void WindowSizeChanged(int width, int height)
    {
    }
}
