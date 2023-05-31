using CK.Core;
using CK.SqlServer;
using static CK.Testing.DBSetupTestHelper;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using CK.DB.Actor;
using CK.DB.Acl;
using System.Collections.Generic;

namespace CK.DB.WebPage.Tests
{
    [TestFixture]
    public class WebPageTests
    {
        [TestCase( -3712 )]
        [TestCase( 0 )]
        public async Task invaid_actor_cannot_create_page_Async( int actorId )
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                await webPageTable
                    .Invoking( async table => await table.CreateWebPageAsync( ctx, actorId, 0, GetNewGuid() ) )
                    .Should().ThrowAsync<Exception>();
            }
        }

        [TestCase( -3712 )]
        [TestCase( int.MaxValue )]
        public async Task invaid_parent_page_throw_an_error_Async( int parentPageId )
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                await webPageTable
                    .Invoking( async table => await table.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid() ) )
                    .Should().ThrowAsync<Exception>();
            }
        }

        [TestCase( "" )]
        [TestCase( "te/st" )]
        [TestCase( "te st" )]
        [TestCase( "te$st" )]
        [TestCase( "te:st" )]
        [TestCase( "te#st" )]
        [TestCase( "te&st" )]
        [TestCase( "te=st" )]
        public async Task invalid_page_name_thow_an_error_Async( string pageName )
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                await webPageTable
                    .Invoking( async table => await table.CreateWebPageAsync( ctx, 1, 0, pageName ) )
                    .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task not_Contributor_on_parent_page_acl_cannot_create_page_Async()
        {
            var userTable = ObtainPackage<UserTable>();
            var webPageTable = ObtainPackage<WebPageTable>();
            var aclTable = ObtainPackage<AclTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int actorId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );

                int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid() );

                int aclId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int>(
                    @"select AclId from CK.tWebPage where PageId = @PageId;",
                    new { PageId = parentPageId } );

                await aclTable.AclGrantSetAsync( ctx, 1, aclId, actorId, "Viewer", 16 );

                await webPageTable
                    .Invoking( async table => await table.CreateWebPageAsync( ctx, actorId, parentPageId, GetNewGuid() ) )
                    .Should().ThrowAsync<Exception>();
            }
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
        public async Task destroy_webPage_with_children_to_1_destroy_all_children_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid() );

                List<int> children = new()
                {
                    await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid() ),
                    await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid() )
                };
                children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[0], GetNewGuid() ) );
                children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[2], GetNewGuid() ) );

                int otherPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid() );

                // Assert pages exists
                (await webPageTable.GetWebPageByIdAsync( ctx, parentPageId )).Should().NotBeNull();
                foreach( int childId in children )
                {
                    (await webPageTable.GetWebPageByIdAsync( ctx, childId )).Should().NotBeNull();
                }
                (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).Should().NotBeNull();

                // Remove
                await webPageTable.DestroyWebPageAsync( ctx, 1, parentPageId, withChildren: true );

                // Assert pages does not exists
                (await webPageTable.GetWebPageByIdAsync( ctx, parentPageId )).Should().BeNull();
                foreach( int childId in children )
                {
                    (await webPageTable.GetWebPageByIdAsync( ctx, childId )).Should().BeNull();
                }
                (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).Should().NotBeNull();
            }
        }

        [Test]
        public async Task destroy_WebPage_with_with_children_and_force_destroy_to_0_throw_an_error_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid() );

                await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid() );

                await webPageTable.Invoking( table => table.DestroyWebPageAsync( ctx, 1, parentPageId, withChildren: false ) )
                                  .Should().ThrowAsync<Exception>();
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

        [Test]
        public async Task rename_WebPage_rename_path_of_children_Async()
        {
            var webPageTable = ObtainPackage<WebPageTable>();

            async Task<string?> GetPagePathNameAsync( ISqlCallContext ctx, int pageId )
            {
                return await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<string>(
                    @"select rp.ResPath
                      from CK.tResPath rp
                      inner join CK.tWebPage wp on rp.ResId = wp.PageId
                      where rp.ResId = @PageId;",
                    new { PageId = pageId } );
            }

            using( SqlStandardCallContext ctx = new() )
            {
                string parentPageName = GetNewGuid();
                var parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, parentPageName );

                (await GetPagePathNameAsync( ctx, parentPageId )).Should().NotBeNull().And.Be( $"P/{parentPageName}" );

                string childPageName = GetNewGuid();
                var childPageId = await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, childPageName );

                (await GetPagePathNameAsync( ctx, childPageId )).Should().NotBeNull().And.Be( $"P/{parentPageName}/{childPageName}" );

                parentPageName = GetNewGuid();
                await webPageTable.RenameWebPageAsync( ctx, 1, parentPageId, parentPageName );

                (await GetPagePathNameAsync( ctx, parentPageId )).Should().NotBeNull().And.Be( $"P/{parentPageName}" );

                (await GetPagePathNameAsync( ctx, childPageId )).Should().NotBeNull().And.Be( $"P/{parentPageName}/{childPageName}" );
            }
        }

        static T ObtainPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} package." );
        }

        /// <summary>
        /// Generate a new guid truncated at 32 characters.
        /// </summary>
        static string GetNewGuid() => Guid.NewGuid().ToString().Substring( 0, 32 );
    }
}
