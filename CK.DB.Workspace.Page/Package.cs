using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.Workspace.Page;

[SqlPackage( Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:CK.vWorkspace, transform:sGroupGroupNameSet" )]
[SqlObjectItem( "fUserSiteMap" )]
public abstract class Package : SqlPackage
{
    void StObjConstruct( CK.DB.WebPage.Package webPage, CK.DB.Workspace.Package workspace )
    {
    }

    /// <summary>
    /// Plug a WebPage to the Workspace.
    /// If the Workspace already have a WebPage, then it do not create a new WebPage.
    /// </summary>
    /// <param name="ctx">The call context.</param>
    /// <param name="actorId">The Actor identifier.</param>
    /// <param name="workspaceId">The Workspace identifier.</param>
    /// <returns>The identifier of the Workspace WebPage.</returns>
    [SqlProcedure( "sWorkspacePagePlug" )]
    public abstract Task<int> PlugWorkspacePageAsync( ISqlCallContext ctx, int actorId, int workspaceId );

    /// <summary>
    /// Unplug the WebPage to the Workspace.
    /// </summary>
    /// <param name="ctx">The call context.</param>
    /// <param name="actorId">The Actor identifier.</param>
    /// <param name="workspaceId">The WorkspaceIdentifier.</param>
    /// <param name="forceUnplug">If <see langword="true"/>, remove recursively all sub WebPage of the Workspace. Else, if <see langword="false"/> the Workspace have sub WebPage, throw an error.</param>
    [SqlProcedure( "sWorkspacePageUnplug" )]
    public abstract Task UnplugWorkspacePageAsync( ISqlCallContext ctx, int actorId, int workspaceId, bool forceUnplug = false );
}
