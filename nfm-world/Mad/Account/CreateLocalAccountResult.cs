using System.Net;

namespace NFMWorld.Accounts;

public class CreateLocalAccountResult(string? username, string message, HttpStatusCode code) : RequestResult(message, code)
{
    public string? Username { get; } = username;

    public override string? ErrorString()
    {
        var current = base.ErrorString();
        if(current is not null) return current;

        switch (StatusCode)
        {
            case HttpStatusCode.Conflict:
                {
                    return "An account already exists with this username, or this account is already registered.";
                }
        }

        return "Unknown error: " + StatusCode;
    }
}