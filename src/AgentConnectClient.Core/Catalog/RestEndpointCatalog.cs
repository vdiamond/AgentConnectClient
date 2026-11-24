namespace AgentConnectClient.Catalog;

/// <summary>
/// The fixed AgentConnect v8 REST endpoint list. Endpoints are not discovered at run time.
/// </summary>
public static class RestEndpointCatalog
{
    static RestEndpointCatalog()
    {
        static ExampleField Field(string name, ExamplePlaceholder placeholder)
        {
            return new ExampleField
            {
                Name = name,
                Placeholder = placeholder,
            };
        }

        static ExampleField GuidField(string name)
        {
            return Field(name, ExamplePlaceholder.Guid);
        }

        static IReadOnlyList<ExampleField> Page()
        {
            return [Field("pageSize", ExamplePlaceholder.Null), Field("continuation", ExamplePlaceholder.Null)];
        }

        All =
        [
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/tenants",
                IsPlatformOperator = true,
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("name", ExamplePlaceholder.TenantName), Field("email", ExamplePlaceholder.OwnerEmail)],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/tenants/{id}/send-invitation",
                IsPlatformOperator = true,
                PathParameters = [GuidField("id")],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/tenants/{id}/suspend",
                IsPlatformOperator = true,
                PathParameters = [GuidField("id")],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/tenants/{id}/resume",
                IsPlatformOperator = true,
                PathParameters = [GuidField("id")],
            },
            new RestEndpointDefinition
            {
                Method = "DELETE",
                Path = "/tenants/{id}",
                IsPlatformOperator = true,
                PathParameters = [GuidField("id")],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/tenant",
                IsPlatformOperator = false,
            },
            new RestEndpointDefinition
            {
                Method = "PATCH",
                Path = "/tenant",
                IsPlatformOperator = false,
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("pageSizeDefault", ExamplePlaceholder.Null), Field("pageSizeMax", ExamplePlaceholder.Null)],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/members",
                IsPlatformOperator = false,
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("email", ExamplePlaceholder.Email)],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/members/{email}/send-invitation",
                IsPlatformOperator = false,
                PathParameters = [Field("email", ExamplePlaceholder.Email)],
            },
            new RestEndpointDefinition
            {
                Method = "DELETE",
                Path = "/members/{email}",
                IsPlatformOperator = false,
                PathParameters = [Field("email", ExamplePlaceholder.Email)],
            },
            new RestEndpointDefinition
            {
                Method = "PUT",
                Path = "/members/{email}/tenant-owner",
                IsPlatformOperator = false,
                PathParameters = [Field("email", ExamplePlaceholder.Email)],
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("isTenantOwner", ExamplePlaceholder.True)],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/members/{email}/token/regenerate",
                IsPlatformOperator = false,
                PathParameters = [Field("email", ExamplePlaceholder.Email)],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/members/{email}/token/send",
                IsPlatformOperator = false,
                PathParameters = [Field("email", ExamplePlaceholder.Email)],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/members",
                IsPlatformOperator = false,
                QueryParameters = Page(),
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/workspaces",
                IsPlatformOperator = false,
                BodyKind = RestBodyKind.Fields,
                BodyFields =
                [
                    Field("name", ExamplePlaceholder.WorkspaceName),
                    Field("description", ExamplePlaceholder.Null),
                    Field("members", ExamplePlaceholder.Null),
                ],
            },
            new RestEndpointDefinition
            {
                Method = "DELETE",
                Path = "/workspaces/{id}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id")],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/workspaces",
                IsPlatformOperator = false,
                QueryParameters = Page(),
            },
            new RestEndpointDefinition
            {
                Method = "PATCH",
                Path = "/workspaces/{id}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id")],
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("name", ExamplePlaceholder.WorkspaceName), Field("description", ExamplePlaceholder.Description)],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/workspaces/{id}/members",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id")],
                QueryParameters = Page(),
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/workspaces/{id}/members",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id")],
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("email", ExamplePlaceholder.Email), Field("role", ExamplePlaceholder.Role)],
            },
            new RestEndpointDefinition
            {
                Method = "DELETE",
                Path = "/workspaces/{id}/members/{email}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), Field("email", ExamplePlaceholder.Email)],
            },
            new RestEndpointDefinition
            {
                Method = "PATCH",
                Path = "/workspaces/{id}/members/{email}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), Field("email", ExamplePlaceholder.Email)],
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("role", ExamplePlaceholder.Role)],
            },
            new RestEndpointDefinition
            {
                Method = "POST",
                Path = "/workspaces/{id}/lists",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id")],
                BodyKind = RestBodyKind.Fields,
                BodyFields =
                [
                    Field("name", ExamplePlaceholder.ListName),
                    Field("description", ExamplePlaceholder.Null),
                    Field("schema", ExamplePlaceholder.EmptyObject),
                ],
            },
            new RestEndpointDefinition
            {
                Method = "PATCH",
                Path = "/workspaces/{id}/lists/{listId}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId")],
                BodyKind = RestBodyKind.Fields,
                BodyFields = [Field("name", ExamplePlaceholder.ListName), Field("description", ExamplePlaceholder.Description)],
            },
            new RestEndpointDefinition
            {
                Method = "DELETE",
                Path = "/workspaces/{id}/lists/{listId}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId")],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/workspaces/{id}/lists",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id")],
                QueryParameters = Page(),
            },
            new RestEndpointDefinition
            {
                Method = "PUT",
                Path = "/workspaces/{id}/lists/{listId}/schema",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId")],
                BodyKind = RestBodyKind.Raw,
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/workspaces/{id}/lists/{listId}/schema",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId")],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/workspaces/{id}/lists/{listId}/items",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId")],
                QueryParameters =
                [
                    Field("shape", ExamplePlaceholder.ShapeFull),
                    Field("pageSize", ExamplePlaceholder.Null),
                    Field("continuation", ExamplePlaceholder.Null),
                    Field("query", ExamplePlaceholder.Null),
                ],
            },
            new RestEndpointDefinition
            {
                Method = "GET",
                Path = "/workspaces/{id}/lists/{listId}/items/{itemId}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId"), GuidField("itemId")],
            },
            new RestEndpointDefinition
            {
                Method = "PUT",
                Path = "/workspaces/{id}/lists/{listId}/items/{itemId}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId"), GuidField("itemId")],
                BodyKind = RestBodyKind.Raw,
            },
            new RestEndpointDefinition
            {
                Method = "DELETE",
                Path = "/workspaces/{id}/lists/{listId}/items/{itemId}",
                IsPlatformOperator = false,
                PathParameters = [GuidField("id"), GuidField("listId"), GuidField("itemId")],
            },
        ];
    }

    /// <summary>
    /// Gets the 32 endpoints in AgentConnect v8 section 6 order.
    /// </summary>
    public static IReadOnlyList<RestEndpointDefinition> All { get; }

    /// <summary>
    /// Finds an endpoint by method and path.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path template.</param>
    /// <returns>The endpoint, or <see langword="null"/> when it is not in the catalog.</returns>
    public static RestEndpointDefinition? Find(string method, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        foreach (var endpoint in All)
        {
            if (string.Equals(endpoint.Method, method, StringComparison.Ordinal)
                && string.Equals(endpoint.Path, path, StringComparison.Ordinal))
            {
                return endpoint;
            }
        }

        return null;
    }
}
