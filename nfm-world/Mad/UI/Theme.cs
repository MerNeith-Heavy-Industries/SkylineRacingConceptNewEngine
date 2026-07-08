using NFMWorld.DriverInterface;
using NFMWorld.Reactor;

namespace NFMWorld.UI;

public class Theme
{
    public static class Colors
    {
        public static readonly Color Primary = new(255, 140, 0);
        public static readonly Color Unimportant = new(180, 180, 180);
        public static readonly Color Background = new(20, 15, 35);
        
        public static readonly Color DarkBg = new(20, 15, 35);
        public static readonly Color ActiveTabBg = new(30, 20, 50);
        public static readonly Color InactiveTabBg = new(15, 5, 30);
        public static readonly Color InputBg = new(30, 25, 50);
        public static readonly Color DiscordBlue = new(88, 101, 242);
        public static readonly Color ErrorRed = new(255, 80, 80);
    }

    public static class Styles
    {
        public static readonly StyleSheet BigButton = Styles(
            flexDirection: FlexDirection.Row,
            alignItems: Align.Center,
            minWidth: 230,
            minHeight: 35,
            padding: MeasurementMultiPadding.XY(12, 8),
            gap: 0,

            backgroundColor: Color.Transparent,
            borderColor: Color.Transparent,
            border: 2,
            borderRadius: 8,
        
            hover: Styles(
                backgroundColor: Colors.Background,
                borderColor: Colors.Primary
            )
        );

        public static readonly StyleSheet BigButtonText = Styles(
            fontStyle: FontStyle.Bold,
            fontSize: 24,
            fontFamily: FontFamily.Adventure,
            foreground: Colors.Primary,
            stroke: Color.Black
        );

        public static readonly StyleSheet Title = Styles(
            fontStyle: FontStyle.Bold,
            fontSize: 48,
            fontFamily: FontFamily.Adventure,
            foreground: Colors.Primary,
            stroke: Color.Black
        );

        public static readonly StyleSheet CardBg = Styles(
            backgroundColor: Colors.DarkBg with { A = 180 },
            borderColor: Colors.Primary with { A = 180 },
            border: 2,
            borderRadius: 8,
                    
            padding: 8,
            gap: 4
        );

        public static readonly StyleSheet TabHeader = Styles(
            padding: MeasurementMultiPadding.XY(12, 10),
            alignItems: Align.Center,
            justifyContent: Justify.Center,

            backgroundColor: Colors.InactiveTabBg,
            borderColor: Colors.Primary,
            borderTopLeftRadius: 6,
            borderTopRightRadius: 6
        );

        public static readonly StyleSheet TabHeaderActive =
        [
            TabHeader,
            Styles(
                backgroundColor: Colors.ActiveTabBg
            )
        ];

        public static readonly StyleSheet TabText = Styles(
            fontFamily: FontFamily.Adventure,
            fontSize: 18,
            fontStyle: FontStyle.Bold,
            foreground: Colors.Unimportant,
            stroke: Color.Black
        );

        public static readonly StyleSheet TabTextActive =
        [
            TabText,
            Styles(
                foreground: Colors.Primary
            )
        ];

        public static readonly StyleSheet TextField = Styles(
            fontSize: 24,
            fontFamily: FontFamily.DroidSans,
            foreground: Color.White,
            placeholderColor: Colors.Unimportant,
            backgroundColor: Colors.InputBg,
            borderColor: Colors.Primary,
            cursorColor: Colors.Primary,
            selectionColor: new Color(100, 180, 255, 128),
            borderRadius: 4,
            padding: MeasurementMultiPadding.XY(10, 8)
        );

        public static readonly StyleSheet SmallButton = Styles(
            alignItems: Align.Center,
            justifyContent: Justify.Center,
            padding: MeasurementMultiPadding.XY(10, 8),
            marginTop: 4,

            backgroundColor: Colors.DarkBg,
            borderColor: Colors.Primary,
            borderRadius: 4
        );

        public static readonly StyleSheet DiscordButton =
        [
            SmallButton,
            Styles(
                backgroundColor: Colors.DiscordBlue,
                borderColor: Colors.DiscordBlue
            )
        ];

        public static readonly StyleSheet SmallButtonText = Styles(
            fontFamily: FontFamily.Adventure,
            fontSize: 18,
            fontStyle: FontStyle.Bold,
            foreground: Colors.Primary
        );

        public static readonly StyleSheet DiscordButtonText =
        [
            SmallButtonText,
            Styles(
                foreground: Color.White
            )
        ];
        
        public static readonly StyleSheet UnimportantButton = Styles(
            alignItems: Align.Center,
            justifyContent: Justify.Center,
            padding: 4
        );
        
        public static readonly StyleSheet UnimportantButtonText =
        [
            SmallButtonText,
            Styles(
                fontSize: 14,
                foreground: Colors.Unimportant
            )
        ];

        public static readonly StyleSheet Form = Styles(
            flexDirection: FlexDirection.Column,
            gap: 12
        );
    }
}