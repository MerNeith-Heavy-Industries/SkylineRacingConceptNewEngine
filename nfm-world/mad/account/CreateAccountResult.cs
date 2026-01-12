namespace nfm_world.mad.account;

public enum CreateAccountResult
{
    /// <summary>
    /// At least for now, account creation is only an intermediate step. The created account needs manual approval
    /// in the database to become officially active.
    /// </summary>
    Success,
    UsernameTaken,
}