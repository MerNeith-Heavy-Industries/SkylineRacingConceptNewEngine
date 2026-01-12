namespace nfm_world.mad.account;

public class AccountManager
{
    public Account? ActiveAccount;

    public async Task<CreateAccountResult> CreateAccount(string username, string password)
    {


        return CreateAccountResult.Success;
    }
}