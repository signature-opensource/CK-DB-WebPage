using CK.SqlServer;
using Dapper;
using System.Threading.Tasks;

namespace CK.DB.WebPage.Tests
{
    public static class WebPageTableExtensions
    {

        public static async Task<WebPage?> GetWebPageByIdAsync( this WebPageTable @this, ISqlCallContext ctx, int pageId )
        {
            return await ctx.GetConnectionController( @this ).QuerySingleOrDefaultAsync<WebPage?>(
                @"select PageId, AclId, PageName, ParentPageId
                      from CK.vWebPage
                      where PageId = @PageId",
                    new { PageId = pageId } );
        }

        public class WebPage
        {
            public int PageId { get; set; }
            public int AclId { get; set; }
            public string? PageName { get; set; }
            public int ParentPageId { get; set; }
        }
    }
}
