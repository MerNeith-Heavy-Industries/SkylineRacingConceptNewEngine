using nfm_world_library.backend;
using nfm_world_library.backend.gamemodes;
using nfm_world.util;

namespace nfm_world.gameplay.gamemodes;

public class FootballClientGamemode(BaseGamemodeParameters gamemodeParameters, IRaceValues raceValues)
    : FootballGamemode(gamemodeParameters, raceValues), IClientGamemode
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