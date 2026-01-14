namespace nfm_world.mad.account.oauth2;

public enum Oauth2CreateAccountResult
{
    Success,
    InvalidCodeOrRedirectURI,
    UsernameTaken,
    InvalidUsername
}