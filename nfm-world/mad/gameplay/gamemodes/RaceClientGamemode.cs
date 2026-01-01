using NFMWorld.Library.backend;
using NFMWorld.Mad.UI.yoga;
using NFMWorld.Util;

namespace NFMWorld.Mad.gamemodes;

public class RaceClientGamemode(BaseGamemodeParameters gamemodeParameters, IRaceValues raceValues)
    : RaceGamemode(gamemodeParameters, raceValues)
{
    
    private PowerDamageBars _pdBars = new PowerDamageBars();

    private static TextBlock _lapText = null!;

    private Node _lapTimerSplits = new Node()
    {
        Name = "LapTimerSplits",
        FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionColumn,
        AlignItems = Yoga.YGAlign.YGAlignFlexStart,
        JustifyContent = Yoga.YGJustify.YGJustifyCenter,
        Gap = 10,
        Padding = 10,

        Children =
        {
            new Node()
            {
                Name = "LapDisplay",
                FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionRow,
                Children =
                {
                    new TextRun()
                    {
                        Name = "LapIcon",
                        Font = new Font(FontFamily.Adventure, 1, 24),
                        Color = new Color(255, 255, 255),
                        StrokeColor = new Color(0, 0, 0),
                        Text = "Lap: ",
                        Flex = 1
                    },
                    new TextBlock()
                    {
                        Ref = textBlock => _lapText = textBlock,
                        StrokeColor = new Color(0, 0, 0),
                        Name = "LapText",
                        Color = new Color(255, 255, 255),
                        Font = new Font(FontFamily.DroidSans, 1, 24),
                        Flex = 1,
                    }
                }
            },
        }
    };

    private static TextRun _centerText = null!;
    private Node _centralTextNode = new Node()
    {
        Name = "CentralText",
        AlignItems = Yoga.YGAlign.YGAlignCenter,
        FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionColumn,

        Children =
        {
            new Node()
            {
                AlignItems = Yoga.YGAlign.YGAlignCenter,
                Flex = 1,
                Children = {
                    new TextRun()
                    {
                        Ref = textBlock => _centerText = textBlock,
                        Text = "",
                        Color = new Color(0, 0, 0, 0),
                        Font = new Font(FontFamily.Adventure, 1, 24),
                        Display = Yoga.YGDisplay.YGDisplayNone
                    },
                }
            },

            new Node()
            {
                Flex = 1
            }
        }
    };

    public void SetLapText(int currentLap)
    {
        _lapText.Text = $"{currentLap + 1}/{currentStage.nlaps}";
    }

}