Makes the workspaces of CK.DB.Workspace hierarchical (CK.DB.HZone): a workspace can be created in a parent workspace
and moved to another one.

Its `WorkspaceTable` adds the parent workspace to `WorkspaceTable.CreateWorkspaceAsync`. Moving a workspace requires
the SafeAdministrator level on it and the Editor level on its current and new parents, and a workspace that has
sub-workspaces cannot be unplugged.
