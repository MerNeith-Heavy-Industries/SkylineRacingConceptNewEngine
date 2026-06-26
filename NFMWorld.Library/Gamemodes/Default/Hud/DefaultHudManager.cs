using NFMWorld.Reactor;
using NFMWorld.UI.Hud;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.UI.Hud.Nodes;
using static NFMWorldLibrary.DriverInterface.UI.Elements.Nodes;

namespace NFMWorldLibrary.Backend.Gamemodes;

[ClientOnly]
public class DefaultHudManager : UIManager, IHud
{
    private readonly ReactorDom _dom;

    private VNode[] _hudElements =
    [
        CentralTextView(),
        PowerDamageBars(),
        LapTimerSplitsView()
    ];

    public HudState State
    {
        get;
        set
        {
            field = value;
            UpdateHud();
        }
    } = new();

    public DefaultHudManager()
    {
        RootPanel = new FlexPanel();
        _dom = new ReactorDom();
        UpdateHud();
    }

    /// <summary>Push current state through the HUD component tree.</summary>
    public void UpdateHud()
    {
        var host = HudHost(state: State, children: _hudElements);
        _dom.Mount(RootPanel, host);
    }

    public void SetElements(params VNode[] elements)
    {
        _hudElements = elements;
        UpdateHud();
    }

    public void AddElement(VNode element)
    {
        _hudElements = [.._hudElements, element];
        UpdateHud();
    }

    public void GameTick()
    {
        State = State with
        {
            DamageColor = GetDamageColor(State.DamageFillAmount),
            PowerColor = GetPowerColor(State.PowerFillAmount)
        };
    }

    #region Power & Damage Bars
    
    public void EventPowerUp(object? sender, float f)
    {
        _powerFlickerTicks = (int)(45*(1/Physics.PHYSICS_MULTIPLIER));
    }

    private static int _damageFlickerTicks = 0;
    private static int _damageFlickerInnerTicks = 0;
    private static bool _damageFlicker = false;

    private static Color GetDamageColor(float fill)
    {
        float cmp = 98f * fill;
        int red = 244;
        int green = 244;
        int blue = 11;

        if (cmp > 33)
            green = (int) (244F - 233F * ((cmp - 33) / 65F));

        /* Handle damage flicker when high - complicated to handle all the tick differences!!! */
        if(cmp > 70)
        {
            if(_damageFlickerTicks < 10*(1/Physics.PHYSICS_MULTIPLIER))
            {
                if(_damageFlickerInnerTicks > (int)(1/Physics.PHYSICS_MULTIPLIER) && _damageFlicker)
                {
                    green = 170;
                    _damageFlicker = false;
                    _damageFlickerInnerTicks = 0;
                } else if (_damageFlickerInnerTicks > (int)(1/Physics.PHYSICS_MULTIPLIER))
                {
                    _damageFlicker = true;
                    _damageFlickerInnerTicks = 0;
                }
                _damageFlickerInnerTicks++;
            } else
            {
                _damageFlickerInnerTicks = 0;
            }
            _damageFlickerTicks++;
            if(_damageFlickerTicks > (167*(1/Physics.PHYSICS_MULTIPLIER)) - cmp * 1.5f) _damageFlickerTicks = 0;
        }

        red = (int)(red + red * (World.Snap[0] / 100F));
        if (red > 255)
            red = 255;
        if (red < 0)
            red = 0;

        green = (int)(green + green * (World.Snap[1] / 100F));
        if (green > 255)
            green = 255;
        if (green < 0)
            green = 0;

        blue = (int)(blue + blue * (World.Snap[2] / 100F));
        if (blue > 255)
            blue = 255;
        if (blue < 0)
            blue = 0;

        return new Color(red, green, blue);
    }

    private static int _powerFlickerTicks = 0;
    private static int _powerFlickerInnerTicks = 0;
    private static bool _powerFlicker = false;

    private static Color GetPowerColor(float fill)
    {
        fill *= 100;

        int red = 128;
        if(fill == 98) red = 64;
        
        int green = (int)(190 + fill * 0.37);
        int blue = 244;

        if(_powerFlickerTicks > 0 && _powerFlickerInnerTicks > (1/Physics.PHYSICS_MULTIPLIER) && _powerFlicker)
        {
            red = 128;
            green = 244;
            blue = 244;
            _powerFlickerInnerTicks = 0;
            _powerFlicker = false;
        } else if(_powerFlickerTicks > 0 && _powerFlickerInnerTicks > (1/Physics.PHYSICS_MULTIPLIER) && !_powerFlicker)
        {
            _powerFlicker = true;
        } else if(_powerFlickerTicks <= 0)
        {
            _powerFlicker = false;
            _powerFlickerInnerTicks = 0;
        }
        _powerFlickerTicks--;
        _powerFlickerInnerTicks++;

        red = (int) (red + red * (World.Snap[0] / 100F));
        if (red > 255) {
            red = 255;
        }
        if (red < 0) {
            red = 0;
        }
        green = (int) (green + green * (World.Snap[1] / 100F));
        if (green > 255) {
            green = 255;
        }
        if (green < 0) {
            green = 0;
        }
        blue = (int) (blue + blue * (World.Snap[2] / 100F));
        if (blue > 255) {
            blue = 255;
        }
        if (blue < 0) {
            blue = 0;
        }

        return new Color(red, green, blue);
    }

    #endregion

    void IHud.LayoutAndRender(Vector2 availableSize, Vector2? origin)
        => LayoutAndRender(availableSize, origin);
}