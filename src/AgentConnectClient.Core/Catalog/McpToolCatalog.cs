namespace AgentConnectClient.Catalog;

/// <summary>
/// The fixed AgentConnect v8 MCP tool list. Tools are not discovered at run time.
/// </summary>
public static class McpToolCatalog
{
    static McpToolCatalog()
    {
        static McpArgument Required(string name, ExamplePlaceholder placeholder)
        {
            return new McpArgument
            {
                Name = name,
                Placeholder = placeholder,
                IsRequired = true,
            };
        }

        static McpArgument Optional(string name)
        {
            return new McpArgument
            {
                Name = name,
                Placeholder = ExamplePlaceholder.Null,
                IsRequired = false,
            };
        }

        static McpArgument OptionalText(string name, ExamplePlaceholder placeholder)
        {
            return new McpArgument
            {
                Name = name,
                Placeholder = placeholder,
                IsRequired = false,
            };
        }

        All =
        [
            new McpToolDefinition
            {
                Name = "list_workspaces",
                Arguments = [Optional("pageSize"), Optional("continuation")],
            },
            new McpToolDefinition
            {
                Name = "update_workspace",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    OptionalText("name", ExamplePlaceholder.WorkspaceName),
                    OptionalText("description", ExamplePlaceholder.Description),
                ],
            },
            new McpToolDefinition
            {
                Name = "list_lists",
                Arguments = [Required("workspaceId", ExamplePlaceholder.Guid), Optional("pageSize"), Optional("continuation")],
            },
            new McpToolDefinition
            {
                Name = "create_list",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("name", ExamplePlaceholder.ListName),
                    Optional("description"),
                    Required("schema", ExamplePlaceholder.EmptyObject),
                ],
            },
            new McpToolDefinition
            {
                Name = "update_list",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("listId", ExamplePlaceholder.Guid),
                    OptionalText("name", ExamplePlaceholder.ListName),
                    OptionalText("description", ExamplePlaceholder.Description),
                ],
            },
            new McpToolDefinition
            {
                Name = "delete_list",
                Arguments = [Required("workspaceId", ExamplePlaceholder.Guid), Required("listId", ExamplePlaceholder.Guid)],
            },
            new McpToolDefinition
            {
                Name = "get_schema",
                Arguments = [Required("workspaceId", ExamplePlaceholder.Guid), Required("listId", ExamplePlaceholder.Guid)],
            },
            new McpToolDefinition
            {
                Name = "update_schema",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("listId", ExamplePlaceholder.Guid),
                    Required("schema", ExamplePlaceholder.EmptyObject),
                ],
            },
            new McpToolDefinition
            {
                Name = "query_items",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("listId", ExamplePlaceholder.Guid),
                    Required("shape", ExamplePlaceholder.ShapeFull),
                    Optional("schemaVersion"),
                    Optional("where"),
                    Optional("orderBy"),
                    Optional("pageSize"),
                    Optional("continuation"),
                ],
            },
            new McpToolDefinition
            {
                Name = "get_item",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("listId", ExamplePlaceholder.Guid),
                    Required("itemId", ExamplePlaceholder.Guid),
                ],
            },
            new McpToolDefinition
            {
                Name = "upsert_item",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("listId", ExamplePlaceholder.Guid),
                    Optional("itemId"),
                    Required("data", ExamplePlaceholder.EmptyObject),
                ],
            },
            new McpToolDefinition
            {
                Name = "delete_item",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("listId", ExamplePlaceholder.Guid),
                    Required("itemId", ExamplePlaceholder.Guid),
                ],
            },
            new McpToolDefinition
            {
                Name = "invite_member",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("email", ExamplePlaceholder.Email),
                    Required("role", ExamplePlaceholder.Role),
                ],
            },
            new McpToolDefinition
            {
                Name = "change_workspace_role",
                Arguments =
                [
                    Required("workspaceId", ExamplePlaceholder.Guid),
                    Required("email", ExamplePlaceholder.Email),
                    Required("role", ExamplePlaceholder.Role),
                ],
            },
            new McpToolDefinition
            {
                Name = "revoke_workspace_access",
                Arguments = [Required("workspaceId", ExamplePlaceholder.Guid), Required("email", ExamplePlaceholder.Email)],
            },
            new McpToolDefinition
            {
                Name = "list_workspace_members",
                Arguments = [Required("workspaceId", ExamplePlaceholder.Guid), Optional("pageSize"), Optional("continuation")],
            },
            new McpToolDefinition
            {
                Name = "set_tenant_owner",
                Arguments = [Required("email", ExamplePlaceholder.Email), Required("isTenantOwner", ExamplePlaceholder.True)],
            },
        ];
    }

    /// <summary>
    /// Gets the 17 tools in AgentConnect v8 FR-33 order.
    /// </summary>
    public static IReadOnlyList<McpToolDefinition> All { get; }

    /// <summary>
    /// Finds a tool by its exact name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns>The tool, or <see langword="null"/> when the name is not in the catalog.</returns>
    public static McpToolDefinition? Find(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        foreach (var tool in All)
        {
            if (string.Equals(tool.Name, name, StringComparison.Ordinal))
            {
                return tool;
            }
        }

        return null;
    }
}
