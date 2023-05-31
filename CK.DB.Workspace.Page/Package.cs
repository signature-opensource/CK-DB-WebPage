using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.Workspace.Page
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:CK.vWorkspace, transform:sGroupGroupNameSet" )]
    [SqlObjectItem( "fUserSiteMap" )]
    public abstract class Package : SqlPackage
    {
        void StObjConstruct( CK.DB.WebPage.Package webPage, CK.DB.Workspace.Package workspace )
        {
        }

        [SqlProcedure( "sWorkspacePagePlug" )]
        public abstract Task PlugWorkspacePageAsync( ISqlCallContext ctx, int actorId, int workspaceId );

        [SqlProcedure( "sWorkspacePageUnplug" )]
        public abstract Task UnplugWorkspacePageAsync( ISqlCallContext ctx, int actorId, int workspaceId, bool forceUnplug = false );
    }
}
