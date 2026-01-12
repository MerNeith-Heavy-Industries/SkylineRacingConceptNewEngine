namespace nfm_world.mad.api;

using ApiRes = (System.Net.HttpStatusCode, ApiResponse);

public static class UserApi
{
    public static async Task<ApiRes> CreateAccount(string username, string password)
    {
        var route = "create_account";
        var body = new
        {
            username,
            password
        };

        return await NfmwApi.PostAsync(route, body);
    }

    public static async Task<ApiRes> LogIn(string username, string password)
    {
        var route = "login";
        var body = new
        {
            username,
            password
        };

        return await NfmwApi.PostAsync(route, body);
    }
}