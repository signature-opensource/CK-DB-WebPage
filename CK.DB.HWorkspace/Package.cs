using CK.Core;

namespace CK.DB.HWorkspace
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sZoneMove" )]
    public abstract class Package : SqlPackage
    {
        void StObjConstruct( CK.DB.Workspace.Package workspace, CK.DB.HZone.Package hZone )
        {
        }
    }
}
