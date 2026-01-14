namespace nfm_world.mad.api;

using System.Net;
using ApiRes = (System.Net.HttpStatusCode, ApiResponse?);

public static class UserApi
{
    public static async Task<ApiRes> CreateLocalAccount(string username, string password)
    {
        var route = "create_account";
        var body = new
        {
            username,
            password
        };

        return await NfmwApi.PostAsync<ApiResponse>(route, body);
    }

    public static async Task<(HttpStatusCode, LogInApiResponse?)> LocalLogIn(string username, string password)
    {
        var route = "login";
        var body = new
        {
            username,
            password
        };

        return await NfmwApi.PostAsync<LogInApiResponse>(route, body);
    }

    public static async Task<ApiRes> CreateDiscordOauth2Account(string oauth2Token, string username)
    {
        var route = "discord_create_account";
        var body = new
        {
            username,
            oauth2Token
        };

        return await NfmwApi.PostAsync<LogInApiResponse>(route, body);
    }

    public static async Task<(HttpStatusCode, LogInApiResponse?)> DiscordOauth2LogIn(string oauth2Token)
    {
        var route = "discord_login";
        var body = new
        {
            oauth2Token
        };

        return await NfmwApi.PostAsync<LogInApiResponse>(route, body);
    }
}