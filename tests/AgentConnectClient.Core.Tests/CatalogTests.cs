using AgentConnectClient.Catalog;

namespace AgentConnectClient.Core.Tests;

/// <summary>
/// The catalogs are checked against AgentConnect Approved v8, not against a second copy inside the library.
/// </summary>
public sealed class CatalogTests
{
    /// <summary>
    /// FR-5 and AgentConnect v8 FR-33: exactly these 17 tools, in this order.
    /// </summary>
    [Fact]
    public void McpTools_MatchAgentConnectV8()
    {
        var expected = new[]
        {
            "list_workspaces",
            "update_workspace",
            "list_lists",
            "create_list",
            "update_list",
            "delete_list",
            "get_schema",
            "update_schema",
            "query_items",
            "get_item",
            "upsert_item",
            "delete_item",
            "invite_member",
            "change_workspace_role",
            "revoke_workspace_access",
            "list_workspace_members",
            "set_tenant_owner",
        };

        Assert.Equal(expected, McpToolCatalog.All.Select(tool => tool.Name).ToArray());
        Assert.Equal(17, McpToolCatalog.All.Count);
        Assert.Equal(expected.Length, expected.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// FR-6 and AgentConnect v8 section 6: exactly these 32 endpoints, in this order.
    /// </summary>
    [Fact]
    public void RestEndpoints_MatchAgentConnectV8()
    {
        var expected = new[]
        {
            "POST /tenants",
            "POST /tenants/{id}/send-invitation",
            "POST /tenants/{id}/suspend",
            "POST /tenants/{id}/resume",
            "DELETE /tenants/{id}",
            "GET /tenant",
            "PATCH /tenant",
            "POST /members",
            "POST /members/{email}/send-invitation",
            "DELETE /members/{email}",
            "PUT /members/{email}/tenant-owner",
            "POST /members/{email}/token/regenerate",
            "POST /members/{email}/token/send",
            "GET /members",
            "POST /workspaces",
            "DELETE /workspaces/{id}",
            "GET /workspaces",
            "PATCH /workspaces/{id}",
            "GET /workspaces/{id}/members",
            "POST /workspaces/{id}/members",
            "DELETE /workspaces/{id}/members/{email}",
            "PATCH /workspaces/{id}/members/{email}",
            "POST /workspaces/{id}/lists",
            "PATCH /workspaces/{id}/lists/{listId}",
            "DELETE /workspaces/{id}/lists/{listId}",
            "GET /workspaces/{id}/lists",
            "PUT /workspaces/{id}/lists/{listId}/schema",
            "GET /workspaces/{id}/lists/{listId}/schema",
            "GET /workspaces/{id}/lists/{listId}/items",
            "GET /workspaces/{id}/lists/{listId}/items/{itemId}",
            "PUT /workspaces/{id}/lists/{listId}/items/{itemId}",
            "DELETE /workspaces/{id}/lists/{listId}/items/{itemId}",
        };

        Assert.Equal(expected, RestEndpointCatalog.All.Select(endpoint => endpoint.Display).ToArray());
        Assert.Equal(32, RestEndpointCatalog.All.Count);
        Assert.Equal(expected.Length, expected.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PlatformOperatorEndpoints_AreTheFiveTenantRoutes()
    {
        var admin = RestEndpointCatalog.All.Where(endpoint => endpoint.IsPlatformOperator).Select(endpoint => endpoint.Display).ToArray();
        Assert.Equal(
            [
                "POST /tenants",
                "POST /tenants/{id}/send-invitation",
                "POST /tenants/{id}/suspend",
                "POST /tenants/{id}/resume",
                "DELETE /tenants/{id}",
            ],
            admin);
        Assert.Equal(27, RestEndpointCatalog.All.Count(endpoint => !endpoint.IsPlatformOperator));
    }
}
