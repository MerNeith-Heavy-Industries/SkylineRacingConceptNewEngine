using ImGuiNET;

namespace nfm_world.mad.account;


/// <summary>
/// FLoating menu for handling logins from various sources.
/// Can be called from a state and queried until logged in.
/// 
/// Can also be used to create an account if one does not exist.
/// </summary>
public class AccountManagerFloatingMenu
{
    public enum AccountManagerFloatingMenuState
    {
        /// <summary>
        /// Any state between logged out and logged in, including creating an account, logging in via Oauth2, etc.
        /// </summary>
        Processing,
        /// <summary>
        /// The user has logged in. The menu can be closed. GameSparker.AccountManager.ActiveAccount should now be non-null
        /// and based on the account that logged in.
        /// </summary>
        LoggedIn,
        /// <summary>
        /// The user closed the menu without logging in. The callsite may wish to cancel the overarchign operation.
        /// </summary>
        Canceled
    }

    private bool _isOpen;

    public AccountManagerFloatingMenu()
    {
        _isOpen = false;
    }

    public void Open()
    {
        _isOpen = true;
    }

    /// <summary>
    /// Render and process the create account/login menu, using ImGui.
    /// </summary>
    /// <returns>The current state of this menu. LoggedIn means the menu can be disposed.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public AccountManagerFloatingMenuState Process()
    {
        if(ImGui.Begin("Login", ref _isOpen, ImGuiWindowFlags.NoCollapse))
        {
            // General design:
            // Display text boxes and text fields for local username/password, then below have a "create account" option,
            // and below that have a "login with Discord" option.
            // "Create Account" opens a submenu where the user can input a username, password, and password confirmation to create a new account.
            // "Login with Discord" should call into the DiscordOauth2Manager flow, and then process that according to the results of
            // the AccountManager Oauth2 methods - if the user is registered, log in, if not, open a submenu where the user can provide a
            // username, and then create an account using that username based on the Discord account.

            
        }

        throw new NotImplementedException();
    }
}