namespace NFMWorldLibrary.Backend.Gamemodes;

/// <summary>
/// Methods with this attribute can only be called from within NFMWorld assembly or with
/// <see cref="ClientServer.RunIfOnClient"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ClientOnlyAttribute : Attribute;