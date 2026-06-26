using System.Net;
using NFMWorld.Api;

namespace NFMWorld.Accounts;

public class AccountManager
{
    public static NfmwClient Client { get; } = new("https://nfmwapi.jacher.io", new HttpClient());
    
    public Account? ActiveAccount;

    public bool LoggedIn { get { return ActiveAccount is not null; } }

    // TODO: Logout properly by querying API to invalidate token?
    public void LogOut()
    {
        ActiveAccount = null;
    }

    /// <summary>
    /// Create an account. On success, this method has no side effects.
    /// Once an account is successfully created, it can be logged into with AccountManager.LogIn
    /// For now, this endpoint requires use of the "master token" set in Authorization. This allows admins
    /// to create accounts.
    /// Oauth2 accounts can be made by anyone but may stil need approval after creation.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <returns>The result of account creation. Throws an exception where there is a server error or input validation error.</returns>
    public async Task<CreateLocalAccountResult> CreateLocalAccount(string username, string password)
    {
        try
        {
            var res = await Client.CreateLocalAccountAsync(new CreateLocalAccountRequest()
            {
                Username = username,
                Password = password
            });

            return new CreateLocalAccountResult(res.Username, "Success", HttpStatusCode.OK);
        }
        catch (ApiException<ErrorResponse> ex)
        {
            return new CreateLocalAccountResult(null, ex.Result.Error, (HttpStatusCode)ex.StatusCode);
        }
    }

    /// <summary>
    /// Log in to a local account. On success, Account property is set to the logged in account.
    /// Account must be null or an exception will be thrown. Call LogOut first to remove the active session
    /// if already logged in.
    /// For Oauth2 accounts use those respective methods.
    /// 
    /// Session token retention policy is that a session token remains valid for a minimum of 24 hours after creation,
    /// and this duration resets every time the session token is used. However, session tokens are force invalidated after
    /// a period of 28 days. They are also invalidated if the user manually revokes any active session tokens.
    /// 
    /// Each user can only have one active session token at a time. If the user logs in from a new source when still logged in,
    /// the prior session token is revoked. This prevents multiple users playing from a single account at a time.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <returns>The log in result. Throws an exception on serious failure.</returns>
    public async Task<LocalLogInResult> LogInToLocalAccount(string username, string password)
    {
        try
        {
            var res = await Client.LoginLocalAccountAsync(new LoginLocalAccountRequest()
            {
                Username = username,
                Password = password
            });

            ActiveAccount = new Account(res.SessionToken, res.Username);
            
            return new LocalLogInResult("Success", HttpStatusCode.OK);
        }
        catch (ApiException<ErrorResponse> ex)
        {
            return new LocalLogInResult(ex.Result.Error, (HttpStatusCode)ex.StatusCode);
        }
    }

    /// <summary>
    /// Update the Account's password. Must have a logged in account to do this.
    /// Only works for a local account (NOT Oauth2)
    /// </summary>
    /// <param name="current">Current password</param>
    /// <param name="updated">New password</param>
    /// <returns>The change password result.</returns>
    public async Task ChangeLocalAccountPassword(string current, string updated)
    {
        throw new NotImplementedException();
    }
}