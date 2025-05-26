using CK.DB.Acl;
using CK.DB.Actor;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CK.Core;

namespace CK.DB.WebPage.Tests;

[TestFixture]
public class WebPageTests
{
    [TestCase( -3712 )]
    [TestCase( 0 )]
    public async Task invalid_actor_cannot_create_page_Async( int actorId )
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            await Util.Awaitable( () => webPageTable.CreateWebPageAsync( ctx, actorId, 0, GetNewGuid(), GetNewGuid() ) )
                .ShouldThrowAsync<Exception>();
        }
    }

    [TestCase( -3712 )]
    [TestCase( int.MaxValue )]
    public async Task invalid_parent_page_throw_an_error_Async( int parentPageId )
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            await Util.Awaitable( () => webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid(), GetNewGuid() ) )
                .ShouldThrowAsync<Exception>();
        }
    }

    [TestCase( "" )]
    [TestCase( "te/st" )]
    [TestCase( "te st" )]
    [TestCase( "te:st" )]
    [TestCase( "te#st" )]
    [TestCase( "te&st" )]
    [TestCase( "te=st" )]
    public async Task invalid_page_name_thow_an_error_Async( string pageName )
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            await Util.Awaitable( () => webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName, pageName ) )
                .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task not_Contributor_on_parent_page_acl_cannot_create_page_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var userTable = services.GetRequiredService<UserTable>();
        var webPageTable = services.GetRequiredService<WebPageTable>();
        var aclTable = services.GetRequiredService<AclTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            int actorId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );

            int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid(), GetNewGuid() );

            int aclId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int>(
                @"select AclId from CK.tWebPage where PageId = @PageId;",
                new { PageId = parentPageId } );

            await aclTable.AclGrantSetAsync( ctx, 1, aclId, actorId, "Viewer", 16 );

            await Util.Awaitable( () => webPageTable.CreateWebPageAsync( ctx, actorId, parentPageId, GetNewGuid(), GetNewGuid() ) )
                .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task create_WebPage_creates_ResPath_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            string pageName = GetNewGuid();
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName, pageName );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBe( pageId );
            webPage!.ResPath.ShouldEndWith( '/' + pageName );
            webPage!.ParentPageId.ShouldBe( 0 );

            int? resId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int?>(
                "select ResId from CK.tResPath where ResId = @ResId",
                new { ResId = pageId } );

            resId.ShouldNotBeNull();
        }
    }

    [Test]
    public async Task create_WebPage_with_dollar_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            string pageName = $"${GetNewGuid( 31 )}";
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName, pageName );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBe( pageId );
            webPage!.ResPath.ShouldEndWith( '/' + pageName );
        }
    }

    [Test]
    public async Task create_WebPage_without_parent_and_acl_creates_new_acl_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            string pageName = GetNewGuid();
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName, pageName );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

            webPage.ShouldNotBeNull();
            webPage!.ParentPageId.ShouldBe( 0 );
            webPage.AclId.ShouldBeGreaterThan( 0 );
        }
    }

    [Test]
    public async Task create_WebPage_with_parent_set_parent_aclId_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            string parentPageName = GetNewGuid();
            int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, parentPageName, parentPageName );

            string pageName = GetNewGuid();
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, pageName, pageName );

            var parentWebPage = await webPageTable.GetWebPageByIdAsync( ctx, parentPageId );
            parentWebPage.ShouldNotBeNull();
            parentWebPage!.PageId.ShouldBe( parentPageId );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );
            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBe( pageId );
            webPage.PageId.ShouldNotBe( parentWebPage.PageId );
            webPage.ResPath.ShouldEndWith( '/' + pageName );
            webPage.ParentPageId.ShouldBe( parentWebPage.PageId );
            webPage.AclId.ShouldBe( parentWebPage.AclId );
        }
    }

    [Test]
    public async Task destroy_WebPage_destroy_Res_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid(), GetNewGuid() );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBe( pageId );

            int? resId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int?>(
                "select ResId from CK.tResPath where ResId = @ResId",
                new { ResId = pageId } );

            resId.ShouldNotBeNull();

            await webPageTable.DestroyWebPageAsync( ctx, 1, pageId );

            webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

            webPage.ShouldBeNull();

            resId = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<int?>(
                "select ResId from CK.tRes where ResId = @ResId",
                new { ResId = pageId } );

            resId.ShouldBeNull();
        }
    }

    [Test]
    public async Task destroy_webPage_with_children_to_1_destroy_all_children_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid(), GetNewGuid() );

            List<int> children =
            [
                await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid(), GetNewGuid() ),
                await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid(), GetNewGuid() )
            ];
            children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[0], GetNewGuid(), GetNewGuid() ) );
            children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[2], GetNewGuid(), GetNewGuid() ) );

            int otherPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid(), GetNewGuid() );

            // Assert pages exists
            (await webPageTable.GetWebPageByIdAsync( ctx, parentPageId )).ShouldNotBeNull();
            foreach( int childId in children )
            {
                (await webPageTable.GetWebPageByIdAsync( ctx, childId )).ShouldNotBeNull();
            }
            (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).ShouldNotBeNull();

            // Remove
            await webPageTable.DestroyWebPageAsync( ctx, 1, parentPageId, withChildren: true );

            // Assert pages does not exists
            (await webPageTable.GetWebPageByIdAsync( ctx, parentPageId )).ShouldBeNull();
            foreach( int childId in children )
            {
                (await webPageTable.GetWebPageByIdAsync( ctx, childId )).ShouldBeNull();
            }
            (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).ShouldNotBeNull();
        }
    }

    [Test]
    public async Task destroy_WebPage_with_with_children_and_force_destroy_to_0_throw_an_error_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            int parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid(), GetNewGuid() );

            await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, GetNewGuid(), GetNewGuid() );

            await Util.Invokable( () => webPageTable.DestroyWebPageAsync( ctx, 1, parentPageId, withChildren: false ) )
                              .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task view_right_informations_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            string pageName = GetNewGuid();
            string pageTitle = GetNewGuid();
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName, pageTitle );

            var page = await webPageTable.GetWebPageByIdAsync( ctx, pageId );

            page.ShouldNotBeNull();
            page!.PageId.ShouldBe( pageId );
            page!.ResPath.ShouldEndWith( '/' + pageName );
            page!.PageTitle.ShouldBe( pageTitle );
            page!.ParentPageId.ShouldBe( 0 );

            var res = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<(int ResId, string ResPath)>(
                "select ResId, ResPath from CK.tResPath where ResId = @ResId",
                new { ResId = pageId } );

            res.ResId.ShouldBe( pageId );
            res.ResPath.ShouldBe( $"P/{pageName}" );

            string childPageName = GetNewGuid();
            string childPageTitle = GetNewGuid();
            int childPageId = await webPageTable.CreateWebPageAsync( ctx, 1, pageId, childPageName, childPageTitle );

            var childPage = await webPageTable.GetWebPageByIdAsync( ctx, childPageId );

            childPage.ShouldNotBeNull();
            childPage!.ParentPageId!.ShouldBe( pageId );
            childPage!.ResPath.ShouldEndWith( '/' + childPageName );
            childPage!.PageTitle!.ShouldBe( childPageTitle );
            childPage!.ParentPageId.ShouldBe( pageId );
            childPage!.AclId.ShouldBe( page.AclId );

            res = await ctx.GetConnectionController( webPageTable ).QuerySingleOrDefaultAsync<(int ResId, string ResPath)>(
                "select ResId, ResPath from CK.tResPath where ResId = @ResId",
                new { ResId = childPageId } );

            res.ResId.ShouldBe( childPageId );
            res.ResPath.ShouldBe( $"P/{pageName}/{childPageName}" );
        }
    }

    [Test]
    public async Task rename_WebPage_rename_path_of_children_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var webPageTable = services.GetRequiredService<WebPageTable>();

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
            var parentPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, parentPageName, parentPageName );

            (await GetPagePathNameAsync( ctx, parentPageId )).ShouldNotBeNull().ShouldBe( $"P/{parentPageName}" );

            string childPageName = GetNewGuid();
            var childPageId = await webPageTable.CreateWebPageAsync( ctx, 1, parentPageId, childPageName, childPageName );

            (await GetPagePathNameAsync(ctx, childPageId)).ShouldNotBeNull().ShouldBe( $"P/{parentPageName}/{childPageName}" );

            parentPageName = GetNewGuid();
            await webPageTable.RenameWebPageAsync( ctx, 1, parentPageId, parentPageName );

            (await GetPagePathNameAsync(ctx, parentPageId)).ShouldNotBeNull().ShouldBe( $"P/{parentPageName}" );

            (await GetPagePathNameAsync(ctx, childPageId)).ShouldNotBeNull().ShouldBe( $"P/{parentPageName}/{childPageName}" );
        }
    }

    /// <summary>
    /// Generate a new guid truncated at 32 characters.
    /// </summary>
    static string GetNewGuid( int length = 32 ) => Guid.NewGuid().ToString().Substring( 0, length );
}
