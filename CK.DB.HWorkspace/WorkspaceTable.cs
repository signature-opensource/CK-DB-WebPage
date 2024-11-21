using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.HWorkspace;

/// <summary>
/// Specializes the <see cref="Workspace.WorkspaceTable"/> to support parent workspace whenever possible.
/// </summary>
[SqlTable( "tWorkspace", Package = typeof( Package ), ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:sWorkspaceUnplug" )]
public abstract class WorkspaceTable : CK.DB.Workspace.WorkspaceTable
{
    void StObjConstruct( CK.DB.Workspace.WorkspaceTable workspaceTable, CK.DB.HZone.ZoneTable zoneTable )
    {
    }

    /// <inheritdoc cref="Workspace.WorkspaceTable.CreateWorkspaceAsync(ISqlCallContext, int, string)"/>
    /// <param name="parentWorkspaceId">The parent workspace identifier.</param>
    [SqlProcedure( "transform:sWorkspaceCreate" )]
    public abstract Task<NamedWorkspace> CreateWorkspaceAsync( ISqlCallContext ctx, int actorId, string workspaceName, int parentWorkspaceId = 0 );
}
