namespace nfm_world.mad.api;

using System.Net;
using nfm_world.mad.account.oauth2;
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

    public static async Task<(HttpStatusCode, LogInApiResponse?)> CreateDiscordOauth2Account(string tempToken, string username)
    {
        var route = "discord/create_account";
        var body = new
        {
            temp_token = tempToken,
            username
        };

        return await NfmwApi.PostAsync<LogInApiResponse>(route, body);
    }

    public static async Task<(HttpStatusCode, Oauth2LogInApiResponse?)> DiscordOauth2LogIn(string oauth2Code, string redirectUri)
    {
        var route = "discord/login";
        var body = new
        {
            redirect_uri = redirectUri,
            code = oauth2Code
        };

        return await NfmwApi.PostAsync<Oauth2LogInApiResponse>(route, body);
    }
}