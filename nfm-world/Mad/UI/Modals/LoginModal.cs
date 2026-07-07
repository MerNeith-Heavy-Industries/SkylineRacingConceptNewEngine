using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.UI;
using NFMWorld.Reactor;
using NFMWorld.Reactor.Events;
using WorldXaml.UI.Yoga;
using static NFMWorld.Reactor.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;
using static NFMWorld.UI.Nodes;

namespace NFMWorld.UI;

public class LoginModal(
    bool isVisible,
    Action<string, string>? onSignIn = null,
    Action<string, string, string>? onSignUp = null,
    Action? onDiscordSignIn = null,
    Action? onClose = null
) : Component
{
    private static readonly Color Orange = new(255, 140, 0, 255);
    private static readonly Color Gray = new(160, 160, 160, 255);
    private static readonly Color DarkBg = new(20, 15, 35, 255);
    private static readonly Color ActiveTabBg = new(30, 20, 50, 255);
    private static readonly Color InactiveTabBg = new(15, 5, 30, 255);
    private static readonly Color InputBg = new(30, 25, 50, 255);
    private static readonly Color DiscordBlue = new(88, 101, 242, 255);
    private static readonly Color ErrorRed = new(255, 80, 80, 255);

    protected override VNode Render()
    {
        var (activeTab, setActiveTab) = UseState(0);
        var (signInUsername, setSignInUsername) = UseState("");
        var (signInPassword, setSignInPassword) = UseState("");
        var (signUpUsername, setSignUpUsername) = UseState("");
        var (signUpEmail, setSignUpEmail) = UseState("");
        var (signUpPassword, setSignUpPassword) = UseState("");
        var (errorMessage, setErrorMessage) = UseState("");

        var switchToSignIn = UseCallback(() =>
        {
            setActiveTab(_ => 0);
            setErrorMessage(_ => "");
        }, []);

        var switchToSignUp = UseCallback(() =>
        {
            setActiveTab(_ => 1);
            setErrorMessage(_ => "");
        }, []);

        var handleSignIn = UseCallback(() =>
        {
            if (string.IsNullOrWhiteSpace(signInUsername) || string.IsNullOrWhiteSpace(signInPassword))
            {
                setErrorMessage(_ => "Please enter both username and password.");
                return;
            }
            setErrorMessage(_ => "");
            onSignIn?.Invoke(signInUsername, signInPassword);
        }, [signInUsername, signInPassword]);

        var handleSignUp = UseCallback(() =>
        {
            if (string.IsNullOrWhiteSpace(signUpUsername) || string.IsNullOrWhiteSpace(signUpEmail) || string.IsNullOrWhiteSpace(signUpPassword))
            {
                setErrorMessage(_ => "Please fill in all fields.");
                return;
            }
            setErrorMessage(_ => "");
            onSignUp?.Invoke(signUpUsername, signUpEmail, signUpPassword);
        }, [signUpUsername, signUpEmail, signUpPassword]);

        var handleDiscord = UseCallback(() =>
        {
            setErrorMessage(_ => "");
            onDiscordSignIn?.Invoke();
        }, []);

        var handleClose = UseCallback(() =>
        {
            onClose?.Invoke();
        }, []);

        return Modal(
            isVisible: isVisible,
            modal: PaintedBox(
                name: "backdrop",
                backgroundColor: new Color(0, 0, 0, 180),
                borderColor: Color.Transparent,

                flexDirection: FlexDirection.Column,
                minWidth: 380,
                padding: 24,
                gap: 16,

                children: [
                    // Card background
                    PaintedBox(
                        name: "card",
                        backgroundColor: DarkBg,
                        borderColor: Orange,
                        border: 2,
                        borderTopLeftRadius: 8,
                        borderTopRightRadius: 8,
                        borderBottomLeftRadius: 8,
                        borderBottomRightRadius: 8,
                        
                        padding: 8,
                        gap: 4,

                        flexDirection: FlexDirection.Column,

                        children: [
                            // Tab bar
                            FlexPanel(
                                name: "tabBar",
                                flexDirection: FlexDirection.Row,
                                gap: 0,
                                marginBottom: 8,
                                children: [
                                    // Sign In tab
                                    PaintedBox(
                                        name: "signInTab",
                                        flex: 1,
                                        padding: "12,10",
                                        alignItems: Align.Center,
                                        justifyContent: Justify.Center,
                                        mousePressed: _ => switchToSignIn(),
                                        
                                        backgroundColor: activeTab == 0 ? ActiveTabBg : InactiveTabBg,
                                        borderColor: Orange,
                                        borderTopLeftRadius: 6,
                                        borderTopRightRadius: 6,
                                        
                                        children: [
                                            TextRun(
                                                name: "signInTabText",
                                                fontFamily: FontFamily.Adventure,
                                                fontSize: 18,
                                                fontStyle: FontStyle.Bold,
                                                foreground: activeTab == 0 ? Orange : Gray,
                                                stroke: Color.Black,
                                                text: "SIGN IN"
                                            )
                                        ]
                                    ),
                                    // Sign Up tab
                                    PaintedBox(
                                        name: "signUpTab",
                                        flex: 1,
                                        padding: "12,10",
                                        alignItems: Align.Center,
                                        justifyContent: Justify.Center,
                                        mousePressed: _ => switchToSignUp(),
                                        
                                        backgroundColor: activeTab == 1 ? ActiveTabBg : InactiveTabBg,
                                        borderColor: Orange,
                                        borderTopLeftRadius: 6,
                                        borderTopRightRadius: 6,
                                        
                                        children: [
                                            TextRun(
                                                name: "signUpTabText",
                                                fontFamily: FontFamily.Adventure,
                                                fontSize: 18,
                                                fontStyle: FontStyle.Bold,
                                                foreground: activeTab == 1 ? Orange : Gray,
                                                stroke: Color.Black,
                                                text: "SIGN UP"
                                            )
                                        ]
                                    )
                                ]
                            ),

                            // Sign In form
                            FlexPanel(
                                name: "signInForm",
                                flexDirection: FlexDirection.Column,
                                gap: 12,
                                display: activeTab == 0 ? Display.Flex : Display.None,
                                children: [
                                    TextInput(
                                        name: "signInUsernameInput",
                                        placeholder: "Username",
                                        text: signInUsername,
                                        textChanged: t => setSignInUsername(_ => t),
                                        fontSize: 24,
                                        fontFamily: FontFamily.DroidSans,
                                        foreground: Color.White,
                                        placeholderColor: Gray,
                                        backgroundColor: InputBg,
                                        borderColor: Orange,
                                        cursorColor: Orange,
                                        selectionColor: new Color(100, 180, 255, 128),
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        padding: "10,8",
                                        submitted: _ => handleSignIn()
                                    ),
                                    TextInput(
                                        name: "signInPasswordInput",
                                        placeholder: "Password",
                                        text: signInPassword,
                                        textChanged: t => setSignInPassword(_ => t),
                                        fontSize: 24,
                                        fontFamily: FontFamily.DroidSans,
                                        foreground: Color.White,
                                        placeholderColor: Gray,
                                        backgroundColor: InputBg,
                                        borderColor: Orange,
                                        cursorColor: Orange,
                                        selectionColor: new Color(100, 180, 255, 128),
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        padding: "10,8",
                                        submitted: _ => handleSignIn()
                                    ),
                                    // Sign In button
                                    PaintedBox(
                                        name: "signInButton",
                                        alignItems: Align.Center,
                                        justifyContent: Justify.Center,
                                        padding: "10,8",
                                        marginTop: 4,
                                        mousePressed: _ => handleSignIn(),
                                        
                                        backgroundColor: Orange,
                                        borderColor: Orange,
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        
                                        children: [
                                            TextRun(
                                                name: "signInBtnText",
                                                fontFamily: FontFamily.Adventure,
                                                fontSize: 18,
                                                fontStyle: FontStyle.Bold,
                                                foreground: DarkBg,
                                                text: "SIGN IN"
                                            )
                                        ]
                                    )
                                ]
                            ),

                            // Sign Up form
                            FlexPanel(
                                name: "signUpForm",
                                flexDirection: FlexDirection.Column,
                                gap: 12,
                                display: activeTab == 1 ? Display.Flex : Display.None,
                                children: [
                                    TextInput(
                                        name: "signUpUsernameInput",
                                        placeholder: "Username",
                                        text: signUpUsername,
                                        textChanged: t => setSignUpUsername(_ => t),
                                        fontSize: 24,
                                        fontFamily: FontFamily.DroidSans,
                                        foreground: Color.White,
                                        placeholderColor: Gray,
                                        backgroundColor: InputBg,
                                        borderColor: Orange,
                                        cursorColor: Orange,
                                        selectionColor: new Color(100, 180, 255, 128),
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        padding: "10,8"
                                    ),
                                    TextInput(
                                        name: "signUpEmailInput",
                                        placeholder: "Email",
                                        text: signUpEmail,
                                        textChanged: t => setSignUpEmail(_ => t),
                                        fontSize: 24,
                                        fontFamily: FontFamily.DroidSans,
                                        foreground: Color.White,
                                        placeholderColor: Gray,
                                        backgroundColor: InputBg,
                                        borderColor: Orange,
                                        cursorColor: Orange,
                                        selectionColor: new Color(100, 180, 255, 128),
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        padding: "10,8"
                                    ),
                                    TextInput(
                                        name: "signUpPasswordInput",
                                        placeholder: "Password",
                                        text: signUpPassword,
                                        textChanged: t => setSignUpPassword(_ => t),
                                        fontSize: 24,
                                        fontFamily: FontFamily.DroidSans,
                                        foreground: Color.White,
                                        placeholderColor: Gray,
                                        backgroundColor: InputBg,
                                        borderColor: Orange,
                                        cursorColor: Orange,
                                        selectionColor: new Color(100, 180, 255, 128),
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        padding: "10,8"
                                    ),
                                    // Sign Up button
                                    PaintedBox(
                                        name: "signUpButton",
                                        alignItems: Align.Center,
                                        justifyContent: Justify.Center,
                                        padding: "10,8",
                                        marginTop: 4,
                                        mousePressed: _ => handleSignUp(),
                                        
                                        backgroundColor: Orange,
                                        borderColor: Orange,
                                        borderTopLeftRadius: 4,
                                        borderTopRightRadius: 4,
                                        borderBottomLeftRadius: 4,
                                        borderBottomRightRadius: 4,
                                        
                                        children: [
                                            TextRun(
                                                name: "signUpBtnText",
                                                fontFamily: FontFamily.Adventure,
                                                fontSize: 18,
                                                fontStyle: FontStyle.Bold,
                                                foreground: DarkBg,
                                                text: "SIGN UP"
                                            )
                                        ]
                                    )
                                ]
                            ),

                            // Error message
                            TextRun(
                                name: "errorMessage",
                                fontFamily: FontFamily.DroidSans,
                                fontSize: 14,
                                foreground: ErrorRed,
                                text: errorMessage
                            ),

                            // Discord button
                            PaintedBox(
                                name: "discordButton",
                                flexDirection: FlexDirection.Row,
                                alignItems: Align.Center,
                                justifyContent: Justify.Center,
                                padding: "10,8",
                                gap: 8,
                                mousePressed: _ => handleDiscord(),
                                
                                backgroundColor: DiscordBlue,
                                borderColor: DiscordBlue,
                                borderTopLeftRadius: 4,
                                borderTopRightRadius: 4,
                                borderBottomLeftRadius: 4,
                                borderBottomRightRadius: 4,
                                
                                children: [
                                    TextRun(
                                        name: "discordBtnText",
                                        fontFamily: FontFamily.Adventure,
                                        fontSize: 14,
                                        foreground: Color.White,
                                        text: "SIGN IN WITH DISCORD"
                                    )
                                ]
                            ),

                            // Close link
                            FlexPanel(
                                name: "closeLink",
                                alignItems: Align.Center,
                                justifyContent: Justify.Center,
                                padding: 4,
                                mousePressed: _ => handleClose(),
                                children: [
                                    TextRun(
                                        name: "closeText",
                                        fontFamily: FontFamily.Adventure,
                                        fontSize: 14,
                                        foreground: Gray,
                                        text: "Cancel"
                                    )
                                ]
                            )
                        ]
                    )
                ]
            )
        );
    }
}