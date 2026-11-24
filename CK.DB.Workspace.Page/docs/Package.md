Gives a web page (CK.DB.WebPage) to the workspaces of CK.DB.Workspace: the `PageId` column of `CK.tWorkspace`, and the
site map of the pages a user can see.

The `Package` plugs a page into a workspace (`Package.PlugWorkspacePageAsync`) and unplugs it
(`Package.UnplugWorkspacePageAsync`); the `CK.fUserSiteMap` function returns the workspace pages, and their children,
that a user can view.
