using CK.Core;

namespace CK.DB.Workspace.Page
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:CK.vWorkspace, transform:sWorkspaceCreate, transform:sWorkspaceDestroy" )]
    [SqlObjectItem( "fUserSiteMap" )]
    public abstract class Package : SqlPackage
    {
        void StObjConstruct( CK.DB.WebPage.Package webPage, CK.DB.Workspace.Package workspace )
        {
        }
    }
}
