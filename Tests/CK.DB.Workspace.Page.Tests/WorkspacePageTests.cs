using CK.DB.Acl;
using CK.DB.Actor;
using CK.DB.WebPage;
using CK.DB.WebPage.Tests;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
using System.Linq;
using CK.Core;

namespace CK.DB.Workspace.Page.Tests;

[TestFixture]
public class WorkspacePageTests
{
    [Test]
    public async Task create_workspace_creates_webPage_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );

            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBeGreaterThan( 0 );
        }
    }

    [Test]
    public async Task workspace_page_have_same_alc_that_workspace_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            int workspaceAclId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                @"select AclId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                new { workspace.WorkspaceId } );

            (await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId ))!.AclId.ShouldBe( workspaceAclId );
        }
    }

    [Test]
    public async Task unplug_workspace_page_with_force_unplug_destroy_all_webPages_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            // Create children webPage
            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            List<int> children =
            [
                await webPageTable.CreateWebPageAsync( ctx, 1, webPage!.PageId, GetNewGuid(), GetNewGuid() ),
                await webPageTable.CreateWebPageAsync( ctx, 1, webPage.PageId, GetNewGuid(), GetNewGuid() ),
            ];
            children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[0], GetNewGuid(), GetNewGuid() ) );
            children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[2], GetNewGuid(), GetNewGuid() ) );
            int otherPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid(), GetNewGuid() );

            // Check if children exists
            foreach( int childId in children )
            {
                (await webPageTable.GetWebPageByIdAsync( ctx, childId )).ShouldNotBeNull();
            }
            (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).ShouldNotBeNull();

            (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 1 )).Count().ShouldBe( 5 );

            // Unplug workspace
            await workspacePagePkg.UnplugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId, forceUnplug: true );

            // Check if children exists
            (await webPageTable.GetWebPageByIdAsync( ctx, webPage.PageId )).ShouldBeNull();
            foreach( int childId in children )
            {
                (await webPageTable.GetWebPageByIdAsync( ctx, childId )).ShouldBeNull();
            }
            (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).ShouldNotBeNull();

            (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 1 )).ShouldBeEmpty();
        }
    }

    [Test]
    public async Task unplug_workspace_page_without_children_WebPage_and_force_unplug_0_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBeGreaterThan( 0 );

            await workspacePagePkg.UnplugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId, forceUnplug: false );

            webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBe( 0 );
        }
    }

    [TestCase( "te st" )]
    [TestCase( "tooLongWorkspaceNameToPlugAsWorkspacePage" )]
    [TestCase( "te*st" )]
    public async Task workspace_with_invalid_webPage_name_cannot_be_used_as_workspacePage_Async( string workspaceName )
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, workspaceName );
            workspace.Name.ShouldStartWith( workspaceName );

            await Util.Invokable( () => workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId ) )
                                  .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task unplug_workspace_with_children_page_thow_an_error_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            await webPageTable.CreateWebPageAsync( ctx, 1, webPage!.PageId, GetNewGuid(), GetNewGuid() );

            await Util.Invokable( () => workspacePagePkg.UnplugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId, forceUnplug: false ) )
                               .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task invalid_user_cannot_obtain_workspace_siteMap_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var userTable = services.GetRequiredService<UserTable>();
        var aclTable = services.GetRequiredService<AclTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            webPage.ShouldNotBeNull();

            // Anonimous user
            (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 0 )).ShouldBeEmpty();


            // Not workspace member
            int userId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );
            (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).ShouldBeEmpty();

            // Not viewer
            await aclTable.AclGrantSetAsync( ctx, 1, webPage!.AclId, userId, "User", 8 );
            (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).ShouldBeEmpty();

            // Viewer
            await aclTable.AclGrantSetAsync( ctx, 1, webPage!.AclId, userId, "Viewer", 16 );
            (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).ShouldHaveSingleItem();
        }
    }

    [Test]
    public async Task workspace_viewer_can_obtain_siteMap_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var userTable = services.GetRequiredService<UserTable>();
        var aclTalbe = services.GetRequiredService<AclTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            int userId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );
            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            await aclTalbe.AclGrantSetAsync( ctx, 1, webPage!.AclId, userId, "Viewer", 16 );

            (await workspaceTable.GetWorkspaceSiteMapAsync(ctx, workspace.WorkspaceId, userId)).ShouldHaveSingleItem();
        }
    }

    [Test]
    public async Task get_workspace_webPage_and_children_pages_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var webPageTable = services.GetRequiredService<WebPageTable>();
        var userTable = services.GetRequiredService<UserTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            List<int> webPageIds =
            [
                (await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId ))!.PageId
            ];

            webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[0], GetNewGuid(), GetNewGuid() ) );
            webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[0], GetNewGuid(), GetNewGuid() ) );
            webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[2], GetNewGuid(), GetNewGuid() ) );
            webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[3], GetNewGuid(), GetNewGuid() ) );

            var sitemap = await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 1 );
            sitemap.Count().ShouldBe( webPageIds.Count );

            foreach( var webPageId in webPageIds )
            {
                sitemap.ShouldContain( siteMapitem => siteMapitem.PageId == webPageId );
            }
        }
    }

    [Test]
    public async Task rename_workspacePage_update_workspace_webPage_ResName_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var groupNamePkg = services.GetRequiredService<Group.SimpleNaming.Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            async Task<string> GetWebPageResPathAsync( int workspaceId )
            {
                return await ctx.GetConnectionController( workspacePagePkg ).QuerySingleOrDefaultAsync<string>(
                @"select rp.ResPath
                          from CK.tResPath rp
                          inner join CK.tWorkspace w on rp.ResId = w.PageId
                          where w.WorkspaceId = @WorkspaceId;",
                    new { WorkspaceId = workspaceId } );
            };

            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            (await GetWebPageResPathAsync( workspace.WorkspaceId )).ShouldEndWith( '/' + workspace.Name );

            string workspaceName = await groupNamePkg.GroupRenameAsync( ctx, 1, workspace.WorkspaceId, GetNewGuid() );

            (await GetWebPageResPathAsync( workspace.WorkspaceId )).ShouldEndWith( '/' + workspaceName );
        }
    }

    [TestCase( "te st" )]
    [TestCase( "te$st" )]
    [TestCase( "tooLongWorkspaceNameToPlugAsWorkspacePage" )]
    [TestCase( "te*st" )]
    public async Task rename_workspacePage_with_invalid_name_throw_an_error_Async( string newWorkspaceName )
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<Package>();
        var groupNamePkg = services.GetRequiredService<Group.SimpleNaming.Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            await Util.Invokable( () => groupNamePkg.GroupRenameAsync( ctx, 1, workspace.WorkspaceId, newWorkspaceName ) )
                              .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task plug_workspace_page_is_idempotent_Async()
    {
        var service = SharedEngine.AutomaticServices;
        var workspaceTable = service.GetRequiredService<WorkspaceTable>();
        var workspacePagePackage = service.GetRequiredService<Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            webPage.ShouldBeEquivalentTo( new WorkspaceTableExtensions.WebPage { PageId = 0, AclId = 0 } );

            int previousPageId = 0;
            for( int i = 0; i < 10; i++ )
            {
                int pageId = await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );
                pageId.ShouldBeGreaterThan( 0 );

                webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
                webPage.ShouldNotBeNull();
                webPage!.PageId.ShouldBe( pageId );

                if( previousPageId > 0 )
                {
                    previousPageId.ShouldBe( pageId );
                }
                previousPageId = pageId;
            }
        }
    }

    [Test]
    public async Task unplug_workspace_page_is_idempotent_Async()
    {
        var service = SharedEngine.AutomaticServices;
        var workspaceTable = service.GetRequiredService<WorkspaceTable>();
        var workspacePagePkg = service.GetRequiredService<Package>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            int pageId = await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            webPage.ShouldNotBeNull();
            webPage!.PageId.ShouldBe( pageId );

            for( int i = 0; i < 10; i++ )
            {
                await workspacePagePkg.UnplugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );
                webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
                webPage.ShouldBeEquivalentTo( new WorkspaceTableExtensions.WebPage { PageId = 0, AclId = 0 } );
            }
        }
    }

    /// <summary>
    /// Generate a new guid truncated at 32 characters.
    /// </summary>
    static string GetNewGuid( int length = 32 ) => Guid.NewGuid().ToString().Substring( 0, length );
}
