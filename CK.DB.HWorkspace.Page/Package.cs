using CK.Core;

namespace CK.DB.HWorkspace.Page
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sZoneMove" )]
    [SqlObjectItem( "transform:sWorkspacePagePlug" )]
    public class Package : SqlPackage
    {
        void StObjConstruct( CK.DB.Workspace.Page.Package workspacePage, CK.DB.HWorkspace.Package hworkspace )
        {
        }
    }
}
