using CK.SqlServer;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.DB.Workspace.Page.Tests
{
    public static class WorkspaceTableExtensions
    {
        public static async Task<IEnumerable<SiteMap>> GetWorkspaceSiteMapAsync( this WorkspaceTable @this, ISqlCallContext ctx, int workspaceId, int userId )
        {
            return await ctx.GetConnectionController( @this ).QueryAsync<SiteMap>(
                @"select usm.WorkspaceId, usm.ResPath, usm.GrantLevel, rp.ResId as PageId
                      from CK.fUserSiteMap(@UserId) usm
                      inner join CK.tResPath rp on usm.ResPath like rp.ResPath
                      where usm.WorkspaceId = @WorkspaceId;",
                new { WorkspaceId = workspaceId, @UserId = userId } );
        }

        public class SiteMap
        {
            public int WorkspaceId { get; set; }
            public string ResPath { get; set; } = string.Empty;
            public byte GrantLevel { get; set; }
            public int PageId { get; set; }
        }

        public static async Task<WebPage?> GetWebPageFromWorkspaceIdAsync( this WorkspaceTable @this, ISqlCallContext ctx, int workspaceId )
        {
            return await ctx.GetConnectionController( @this ).QuerySingleOrDefaultAsync<WebPage?>(
                @"select wp.PageId, wp.AclId
                      from CK.tWorkspace w
                      inner join CK.tWebPage wp on w.PageId = wp.PageId
                      where w.WorkspaceId = @WorkspaceId;",
                new { WorkspaceId = workspaceId } );
        }

        public class WebPage
        {
            public int PageId { get; set; }
            public int AclId { get; set; }
        }
    }
}
