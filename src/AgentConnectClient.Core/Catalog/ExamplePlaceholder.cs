namespace AgentConnectClient.Catalog;

/// <summary>
/// The kind of placeholder written into example JSON.
/// </summary>
public enum ExamplePlaceholder
{
    /// <summary>
    /// JSON null, which means the value is not given.
    /// </summary>
    Null,

    /// <summary>
    /// A placeholder GUID string.
    /// </summary>
    Guid,

    /// <summary>
    /// A placeholder member email.
    /// </summary>
    Email,

    /// <summary>
    /// The placeholder email used for the first Tenant owner.
    /// </summary>
    OwnerEmail,

    /// <summary>
    /// A placeholder Tenant name.
    /// </summary>
    TenantName,

    /// <summary>
    /// A placeholder Workspace name.
    /// </summary>
    WorkspaceName,

    /// <summary>
    /// A placeholder List name.
    /// </summary>
    ListName,

    /// <summary>
    /// A placeholder description string.
    /// </summary>
    Description,

    /// <summary>
    /// A placeholder Workspace role.
    /// </summary>
    Role,

    /// <summary>
    /// The query shape <c>full</c>.
    /// </summary>
    ShapeFull,

    /// <summary>
    /// JSON true.
    /// </summary>
    True,

    /// <summary>
    /// An empty JSON object.
    /// </summary>
    EmptyObject,
}
