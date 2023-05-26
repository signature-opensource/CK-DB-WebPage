using CK.Core;
using CK.SqlServer;
using static CK.Testing.DBSetupTestHelper;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CK.DB.WebPage.Tests
{
    [TestFixture]
    public class WebPageTests
    {
        [TestCase( null )]
        [TestCase( -3712 )]
        [TestCase( 0 )]
        public async Task invaid_actor_cannot_create_page_Async( int? actorId )
        {
        }

        [TestCase( "" )]
        public async Task invalid_page_name_thow_an_error_Async( string pageName )
        {
        }

        [Test]
        public async Task not_Contributor_on_parent_page_acl_cannot_create_page_Async()
        {
        }

        [Test]
        public async Task create_WebPage_creates_ResPath_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string pageName = GetNewGuid();
                int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName );

                var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

                webPage.Should().NotBeNull();
                webPage!.PageId.Should().Be( pageId );
                webPage!.PageName.Should().Be( pageName );
                webPage!.ParentPageId.Should().Be( 0 );

                int? resId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int?>(
                    "select ResId from CK.tResPath where ResId = @ResId",
                    new { ResId = pageId } );

                resId.Should().NotBeNull();
            }
        }

        [Test]
        public async Task create_WebPage_without_parent_and_acl_creates_new_acl_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string pageName = GetNewGuid();
                int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName );

                var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

                webPage.Should().NotBeNull();
                webPage!.ParentPageId.Should().Be( 0 );
                webPage.AclId.Should().BeGreaterThan( 0 );
            }
        }

        [Test]
        public async Task create_WebPage_with_parent_set_parent_aclId_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string parentPageName = GetNewGuid();
                int parentPageId = await webPageTable.CreateWebPageAsync(ctx, 1, 0, parentPageName);

                string pageName = GetNewGuid();
                int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, pageName );

                var parentWebPage = await webPageTable.GetWebPageByIdAsync( ctx, parentPageId );
                parentWebPage.Should().NotBeNull();
                parentWebPage!.PageId.Should().Be( parentPageId );

                var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );
                webPage.Should().NotBeNull();
                webPage!.PageId.Should().Be( pageId );
                webPage.PageId.Should().NotBe( parentWebPage.PageId );
                webPage.PageName.Should().Be( pageName );
                webPage.ParentPageId.Should().Be( parentWebPage.PageId );
                webPage.AclId.Should().Be( parentWebPage.AclId );
            }
        }

        [Test]
        public async Task destroy_WebPage_destroy_ResPath_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid() );

                var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

                webPage.Should().NotBeNull();
                webPage!.PageId.Should().Be( pageId );

                int? resId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int?>(
                    "select ResId from CK.tResPath where ResId = @ResId",
                    new { ResId = pageId } );

                resId.Should().NotBeNull();

                await webPageTable.DestroyWebPageAsync( ctx, 1, pageId );

                webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

                webPage.Should().BeNull();

                resId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int?>(
                    "select ResId from CK.tResPath where ResId = @ResId",
                    new { ResId = pageId } );

                resId.Should().BeNull();
            }
        }

        [Test]
        public async Task view_right_informations_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string pageName = GetNewGuid();
                int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName );

                var page = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

                page.Should().NotBeNull();
                page!.PageId.Should().Be( pageId );
                page!.PageName.Should().Be( pageName );
                page!.ParentPageId.Should().Be( 0 );

                var res = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<(int ResId, string ResPath)>(
                    "select ResId, ResPath from CK.tResPath where ResId = @ResId",
                    new { ResId = pageId } );

                res.Should().NotBeNull();
                res.ResId.Should().Be( pageId );
                res.ResPath.Should().Be( $"P/{pageName}" );

                string childPageName = GetNewGuid();
                int childPageId = await webPageTable.CreateWebPageAsync( ctx, 1, pageId, childPageName );

                var childPage = await webPageTable.GetWebPageByIdAsync( ctx, childPageId );

                childPage.Should().NotBeNull();
                childPage!.ParentPageId!.Should().Be( pageId );
                childPage!.PageName.Should().Be( childPageName );
                childPage!.ParentPageId.Should().Be( pageId );
                childPage!.AclId.Should().Be( page.AclId );

                res = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<(int ResId, string ResPath)>(
                    "select ResId, ResPath from CK.tResPath where ResId = @ResId",
                    new { ResId = childPageId } );

                res.Should().NotBeNull();
                res.ResId.Should().Be( childPageId );
                res.ResPath.Should().Be( $"P/{pageName}/{childPageName}" );
            }
        }

        static T ObtainPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} package." );
        }

        static string GetNewGuid() => Guid.NewGuid().ToString().Substring( 0, 32 );
    }
}
