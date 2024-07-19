using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.WebPage
{
    [SqlTable( "tWebPage", Package = typeof( Package ), ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "vWebPage" )]
    public abstract class WebPageTable : SqlTable
    {
        void StObjConstruct( CK.DB.Res.ResPath.Package resPath, CK.DB.Acl.Package acl, CK.DB.Res.ResString.Package resString )
        {
        }

        /// <summary>
        /// Creates a new WebPage.
        /// This is (by default) possible for parent page Contributor (i.e. the <paramref name="actorId"/> must have Contributor level (32) on the parent page Acl).
        /// If <paramref name="aclId"/> is 0 or <see langword="null"/>, the parent page Acl will be used.
        /// And if <paramref name="parentPageId"/> is 0, a new Acl will be created.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting user.</param>
        /// <param name="parentPageId">The parent page identifier.</param>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="aclId">The Acl of the page</param>
        /// <returns>The identifier of the page.</returns>
        [SqlProcedure( "sWebPageCreate" )]
        public abstract Task<int> CreateWebPageAsync( ISqlCallContext ctx, int actorId, int parentPageId, string pageName, string pageTitle, int aclId = 0 );

        /// <summary>
        /// Destroy the WebPage.
        /// This is possible only for page Administrator (i.e. the <paramref name="actorId"/> must have Administrator level (127) on the page Acl).
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="pageId">The acting user.</param>
        /// <param name="withChildren">If <see langword="true"/>, remove recursively all children of the webPage and the page. Else, remove only the page.</param>
        [SqlProcedure( "sWebPageDestroy" )]
        public abstract Task DestroyWebPageAsync( ISqlCallContext ctx, int actorId, int pageId, bool withChildren = false );

        /// <summary>
        /// Rename the WebPage and all the children web page path.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting user.</param>
        /// <param name="pageId">The page to rename.</param>
        /// <param name="newPageName">The new name of the page.</param>
        /// <returns>The new page path.</returns>
        [SqlProcedure( "sWebPageRename" )]
        public abstract Task<string> RenameWebPageAsync( ISqlCallContext ctx, int actorId, int pageId, string newPageName );
    }
}
