namespace nfm_world.mad.account;

public enum LogInResult
{
    /// <summary>
    /// On success, AccountManager.ActiveAccount is set to an Account and the token is stored there.
    /// The token is also written to the operating system keyring for access later.
    /// 
    /// Tokens have a maximum idle time before being invalidated; if it is invalidated, the user must log in again.
    /// </summary>
    Success,
    Unauthorized,
    ServerError,
    ClientError,
    /// <summary>
    /// This happens if the user invokes a password reset request.
    /// </summary>
    MustChangePasswordBeforeLogIn,
    AccountNotApproved
}