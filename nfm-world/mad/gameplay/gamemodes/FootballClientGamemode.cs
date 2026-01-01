using NFMWorld.Library.backend;
using NFMWorld.Util;

namespace NFMWorld.Mad.gamemodes;

public class FootballClientGamemode(BaseGamemodeParameters gamemodeParameters, IRaceValues raceValues)
    : FootballGamemode(gamemodeParameters, raceValues)
{
    public void KeyPressed(Keys key)
    {
        // Handle key presses specific to Time Trial mode
        if (key == Keys.R)
        {
            Reset();
        }
    }

    public void KeyReleased(Keys key)
    {
        // Handle key releases specific to Time Trial mode
    }

    public void Render()
    {
    }
}