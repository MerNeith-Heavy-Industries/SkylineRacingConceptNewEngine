namespace NFMWorld.Accounts;

public class Account
{
    public string? Token;
    public string Username;

    public Account(string token, string username)
    {
        Token = token;
        Username = username;
    }
}