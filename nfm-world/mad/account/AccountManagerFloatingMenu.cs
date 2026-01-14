using ImGuiNET;
using System;
using System.Threading;
using System.Threading.Tasks;
using nfm_world.mad.account.oauth2;

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
        Canceled,
    }

    private bool _isOpen;
    private string _localUsername = string.Empty;
    private string _localPassword = string.Empty;

    private bool _showCreateMenu = false;
    private string _createUsername = string.Empty;
    private string _createPassword = string.Empty;
    private string _createPasswordConfirm = string.Empty;

    // OAuth state
    private Task<DiscordOauth2Manager.DiscordOauthResult>? _oauthTask;
    private DiscordOauth2Manager.DiscordOauthResult? _oauthResult;
    private string _statusMessage = string.Empty;
    private bool _loggedIn = false;
    private CancellationTokenSource? _oauthCts;

    public AccountManagerFloatingMenu()
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
        // Close window handler: if the UI was closed while OAuth is running, cancel it
        if (!_isOpen)
        {
            try
            {
                if (_oauthCts != null && !(_oauthTask?.IsCompleted ?? true))
                {
                    _oauthCts.Cancel();
                }
            }
            catch { }
            finally
            {
                try { _oauthCts?.Dispose(); } catch { }
                _oauthCts = null;
            }

            return _loggedIn ? AccountManagerFloatingMenuState.LoggedIn : AccountManagerFloatingMenuState.Canceled;
        }

        if (ImGui.Begin("Login", ref _isOpen, ImGuiWindowFlags.NoCollapse))
        {
            if (!_showCreateMenu)
            {
                ImGui.Text("Local login");
                ImGui.InputText("Username", ref _localUsername, 128);
                ImGui.InputText("Password", ref _localPassword, 128, ImGuiInputTextFlags.Password);
                if (ImGui.Button("Login"))
                {
                    // TODO: call the real local login API (GameSparker.AccountManager or similar)
                    _statusMessage = $"(stub) Attempted local login for {_localUsername}";
                }

                ImGui.NewLine();
                ImGui.Spacing();
                if (ImGui.Button("Create Account"))
                {
                    _showCreateMenu = true;
                }
            }
            else
            {
                // When in Create mode hide the login controls and show a Log In toggle
                ImGui.Text("Create Account");

                ImGui.InputText("New username", ref _createUsername, 128);
                ImGui.InputText("New password", ref _createPassword, 128, ImGuiInputTextFlags.Password);
                ImGui.InputText("Confirm password", ref _createPasswordConfirm, 128, ImGuiInputTextFlags.Password);
                if (ImGui.Button("Create"))
                {
                    if (string.IsNullOrWhiteSpace(_createUsername))
                        _statusMessage = "Username required";
                    else if (_createPassword != _createPasswordConfirm)
                        _statusMessage = "Passwords do not match";
                    else
                    {
                        // TODO: call account creation API
                        _statusMessage = $"(stub) Created account {_createUsername}";
                        _showCreateMenu = false;
                    }
                }

                ImGui.NewLine();
                ImGui.Spacing();
                if (ImGui.Button("Log In"))
                {
                    _showCreateMenu = false;
                }
            }

            ImGui.Separator();
            ImGui.Text("Social login");
            if (ImGui.Button("Login with Discord"))
            {
                if (_oauthTask == null || _oauthTask.IsCompleted)
                {
                    // Cancel any existing CTS and create a new one for this flow
                    try { _oauthCts?.Cancel(); } catch { }
                    try { _oauthCts?.Dispose(); } catch { }
                    _oauthCts = new CancellationTokenSource();
                    var token = _oauthCts.Token;

                    // Kick off OAuth2 flow in background. Replace CLIENT_ID placeholder with real value.
                    var clientId = "YOUR_DISCORD_CLIENT_ID";
                    _statusMessage = "Opening Discord in web browser.";
                    _oauthResult = null;
                    var manager = new DiscordOauth2Manager();
                    _oauthTask = manager.AuthorizeAsync(clientId, ["identify", "email"], token);
                }
            }

            if (_oauthTask != null)
            {
                if (!_oauthTask.IsCompleted)
                {
                    ImGui.Text("OAuth: in progress...");
                }
                else
                {
                    _oauthResult ??= _oauthTask.Result;
                    if (_oauthResult != null)
                    {
                        if (_oauthResult.Success)
                        {
                            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "OAuth succeeded");
                            ImGui.Text($"Code: {_oauthResult.Code}");
                            ImGui.Text($"RedirectUri: {_oauthResult.RedirectUri}");
                            _statusMessage = "Received OAuth code. Hand this to your backend to exchange for tokens.";
                            // For this draft we mark logged in when we receive a code. In real usage validate on backend.
                            _loggedIn = true;
                        }
                        else
                        {
                            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "OAuth failed");
                            ImGui.Text($"Error: {_oauthResult.Error}");
                            _statusMessage = "OAuth failed or cancelled.";
                        }
                    }
                }
            }

            ImGui.Separator();
            if (!string.IsNullOrEmpty(_statusMessage))
                ImGui.Text(_statusMessage);

            ImGui.End();
        }

        // If still open and not yet logged in, remain in Processing
        if (_loggedIn)
            return AccountManagerFloatingMenuState.LoggedIn;
        return AccountManagerFloatingMenuState.Processing;
    }
}