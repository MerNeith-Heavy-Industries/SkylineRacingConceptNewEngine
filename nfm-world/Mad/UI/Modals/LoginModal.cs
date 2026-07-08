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
            modal:
                // Card background
                PaintedBox(
                    name: "card",
                    
                    style: Theme.Styles.CardBg,
                    
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
                                    style: activeTab == 0
                                        ? Theme.Styles.TabHeaderActive
                                        : Theme.Styles.TabHeader,
                                    mousePressed: _ => switchToSignIn(),
                                    
                                    children: [
                                        TextRun(
                                            name: "signInTabText",
                                            style: activeTab == 0
                                                ? Theme.Styles.TabTextActive
                                                : Theme.Styles.TabText,
                                            text: "SIGN IN"
                                        )
                                    ]
                                ),
                                // Sign Up tab
                                PaintedBox(
                                    name: "signUpTab",
                                    flex: 1,
                                    style: activeTab == 0
                                        ? Theme.Styles.TabHeaderActive
                                        : Theme.Styles.TabHeader,
                                    mousePressed: _ => switchToSignUp(),
                                    
                                    children: [
                                        TextRun(
                                            name: "signUpTabText",
                                            style: activeTab == 0
                                                ? Theme.Styles.TabTextActive
                                                : Theme.Styles.TabText,
                                            text: "SIGN UP"
                                        )
                                    ]
                                )
                            ]
                        ),

                        // Sign In form
                        FlexPanel(
                            name: "signInForm",
                            style: Theme.Styles.Form,
                            display: activeTab == 0 ? Display.Flex : Display.None,
                            children: [
                                TextInput(
                                    name: "signInUsernameInput",
                                    style: Theme.Styles.TextField,
                                    placeholder: "Username",
                                    text: signInUsername,
                                    textChanged: t => setSignInUsername(_ => t),
                                    submitted: _ => handleSignIn()
                                ),
                                TextInput(
                                    name: "signInPasswordInput",
                                    style: Theme.Styles.TextField,
                                    placeholder: "Password",
                                    text: signInPassword,
                                    textChanged: t => setSignInPassword(_ => t),
                                    submitted: _ => handleSignIn()
                                ),
                                // Sign In button
                                PaintedBox(
                                    name: "signInButton",
                                    style: Theme.Styles.SmallButton,
                                    mousePressed: _ => handleSignIn(),
                                    
                                    children: [
                                        TextRun(
                                            name: "signInBtnText",
                                            style: Theme.Styles.SmallButtonText,
                                            text: "SIGN IN"
                                        )
                                    ]
                                )
                            ]
                        ),

                        // Sign Up form
                        FlexPanel(
                            name: "signUpForm",
                            style: Theme.Styles.Form,
                            display: activeTab == 1 ? Display.Flex : Display.None,
                            children: [
                                TextInput(
                                    name: "signUpUsernameInput",
                                    style: Theme.Styles.TextField,
                                    placeholder: "Username",
                                    text: signUpUsername,
                                    textChanged: t => setSignUpUsername(_ => t)
                                ),
                                TextInput(
                                    name: "signUpEmailInput",
                                    style: Theme.Styles.TextField,
                                    placeholder: "Email",
                                    text: signUpEmail,
                                    textChanged: t => setSignUpEmail(_ => t)
                                ),
                                TextInput(
                                    name: "signUpPasswordInput",
                                    style: Theme.Styles.TextField,
                                    placeholder: "Password",
                                    text: signUpPassword,
                                    textChanged: t => setSignUpPassword(_ => t)
                                ),
                                // Sign Up button
                                PaintedBox(
                                    name: "signUpButton",
                                    style: Theme.Styles.SmallButton,
                                    mousePressed: _ => handleSignUp(),
                                    
                                    children: [
                                        TextRun(
                                            name: "signUpBtnText",
                                            style: Theme.Styles.SmallButtonText,
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
                            foreground: Theme.Colors.ErrorRed,
                            text: errorMessage
                        ),

                        // Discord button
                        PaintedBox(
                            name: "discordButton",
                            style: Theme.Styles.DiscordButton,
                            mousePressed: _ => handleDiscord(),
                            
                            children: [
                                TextRun(
                                    name: "discordBtnText",
                                    style: Theme.Styles.DiscordButtonText,
                                    text: "SIGN IN WITH DISCORD"
                                )
                            ]
                        ),

                        // Close link
                        FlexPanel(
                            name: "closeLink",
                            style: Theme.Styles.UnimportantButton,
                            mousePressed: _ => handleClose(),
                            children: [
                                TextRun(
                                    name: "closeText",
                                    style: Theme.Styles.UnimportantButtonText,
                                    text: "Cancel"
                                )
                            ]
                        )
                    ]
                )
        );
    }
}