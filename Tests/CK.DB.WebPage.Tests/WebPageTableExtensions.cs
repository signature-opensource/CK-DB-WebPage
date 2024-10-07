using CK.SqlServer;
using Dapper;
using System.Threading.Tasks;

namespace CK.DB.WebPage.Tests;

public static class WebPageTableExtensions
{
    public static async Task<WebPage?> GetWebPageByIdAsync( this WebPageTable @this, ISqlCallContext ctx, int pageId )
    {
        return await ctx.GetConnectionController( @this ).QuerySingleOrDefaultAsync<WebPage?>(
            @"select wp.PageId
                        ,wp.AclId
                        ,rp.ResPath
                        ,rppp.ParentResId as ParentPageId
                        ,rs.[Value] as PageTitle
                      from CK.tWebPage wp
                      inner join CK.tResPath rp
                          on wp.PageId = rp.ResId
                      inner join CK.tResString rs
                          on wp.PageId = rs.ResId
                      left join CK.vResPathParentPrefixes rppp
                          on rp.ResId = rppp.ResId and rppp.ParentLevel = 1
                      where wp.PageId = @PageId;",
                new { PageId = pageId } );
    }

    public class WebPage
    {
        public int PageId { get; set; }
        public int AclId { get; set; }
        public string ResPath { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public int ParentPageId { get; set; }
    }
}
