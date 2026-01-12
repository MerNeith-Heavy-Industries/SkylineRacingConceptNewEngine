using System.Net;
using nfm_world.mad.api;

namespace nfm_world.mad.account;

public class AccountManager
{
    public Account? ActiveAccount;

    /// <summary>
    /// Create an account. On success, this method has no side effects.
    /// Once an account is successfulyl created, it can be logged into with AccountManager.LogIn
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <returns>The result of account creation. Throws an exception where there is a server error or input validation error.</returns>
    public async Task<CreateAccountResult> CreateAccount(string username, string password)
    {
        var res = await UserApi.CreateAccount(username, password);

        if(res.Item1 == HttpStatusCode.InternalServerError)
        {
           throw new HttpRequestException(res.Item2.status);
        } else if(res.Item1 == HttpStatusCode.BadRequest)
        {
            throw new HttpRequestException(res.Item2.status);
        } else if(res.Item1 == HttpStatusCode.Conflict)
        {
            return CreateAccountResult.UsernameTaken;
        }

        return CreateAccountResult.Success;
    }
}