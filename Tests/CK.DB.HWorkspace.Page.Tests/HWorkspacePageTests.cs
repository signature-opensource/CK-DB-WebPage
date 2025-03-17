using CK.DB.WebPage.Tests;
using CK.DB.Workspace.Page.Tests;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
using CK.Core;

namespace CK.DB.HWorkspace.Page.Tests;

[TestFixture]
public class HWorkspacePageTests
{
    [Test]
    public async Task sub_workspace_has_parent_workspace_page_name_as_prefix_of_his_page_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var workspacePagePackage = services.GetRequiredService<CK.DB.Workspace.Page.Package>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            string parentWorkspaceName = GetNewGuid();
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, parentWorkspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, parentWorkspace.WorkspaceId );

            var parentWebPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, parentWorkspace.WorkspaceId );
            parentWebPage.ShouldNotBeNull();

            string workspaceName = GetNewGuid();
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, workspaceName, parentWorkspace.WorkspaceId );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            webPage.ShouldNotBeNull();

            string resPath = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<string>(
                "select ResPath from CK.tResPath where ResId = @PageId;",
                new { webPage!.PageId } );

            resPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}" );
        }
    }

    [Test]
    public async Task move_sub_workspace_rename_his_ResPath_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var workspacePagePackage = services.GetRequiredService<CK.DB.Workspace.Page.Package>();
        var webPageTable = services.GetRequiredService<CK.DB.WebPage.WebPageTable>();
        var zoneTable = services.GetRequiredService<CK.DB.HZone.ZoneTable>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            // Setup workspaces and pages

            var parentWorkspaceName = GetNewGuid();
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, parentWorkspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, parentWorkspace.WorkspaceId );

            var workspaceName = GetNewGuid();
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, workspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );
            var workspacePage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            await zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, parentWorkspace.WorkspaceId );

            (await webPageTable.GetWebPageByIdAsync( ctx, workspacePage!.PageId ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}" );

            // Create new workspace and move

            var newParentWorkspaceName = GetNewGuid();
            var newParentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, newParentWorkspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, newParentWorkspace.WorkspaceId );

            await zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, newParentWorkspace.WorkspaceId );

            (await webPageTable.GetWebPageByIdAsync( ctx, workspacePage.PageId ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{newParentWorkspaceName}/{workspaceName}" );
        }
    }

    [Test]
    public async Task move_sub_workspace_rename_his_children_page_ResPath_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var workspacePagePackage = services.GetRequiredService<CK.DB.Workspace.Page.Package>();
        var webPageTable = services.GetRequiredService<CK.DB.WebPage.WebPageTable>();
        var zoneTable = services.GetRequiredService<CK.DB.HZone.ZoneTable>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            // Setup workspaces and pages

            var parentWorkspaceName = GetNewGuid( 20 );
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, parentWorkspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, parentWorkspace.WorkspaceId );

            var workspaceName = GetNewGuid( 20 );
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, workspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );
            var workspacePage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
            await zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, parentWorkspace.WorkspaceId );

            var childPage1Name = GetNewGuid( 20 );
            int child1PageId = await webPageTable.CreateWebPageAsync( ctx, 1, workspacePage!.PageId, childPage1Name, childPage1Name );

            var childPage2Name = GetNewGuid( 20 );
            int childPage2Id = await webPageTable.CreateWebPageAsync( ctx, 1, child1PageId, childPage2Name, childPage1Name );

            (await webPageTable.GetWebPageByIdAsync( ctx, workspacePage.PageId ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}" );

            (await webPageTable.GetWebPageByIdAsync( ctx, child1PageId ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}/{childPage1Name}" );
            (await webPageTable.GetWebPageByIdAsync( ctx, childPage2Id ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}/{childPage1Name}/{childPage2Name}" );

            // Create new workspace and move
            var newParentWorkspaceName = GetNewGuid( 20 );
            var newParentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, newParentWorkspaceName );
            await workspacePagePackage.PlugWorkspacePageAsync( ctx, 1, newParentWorkspace.WorkspaceId );

            await zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, newParentWorkspace.WorkspaceId );

            (await webPageTable.GetWebPageByIdAsync( ctx, workspacePage.PageId ))
                      .ShouldNotBeNull().ResPath.ShouldBe( $"P/{newParentWorkspaceName}/{workspaceName}" );
            (await webPageTable.GetWebPageByIdAsync( ctx, child1PageId ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{newParentWorkspaceName}/{workspaceName}/{childPage1Name}" );
            (await webPageTable.GetWebPageByIdAsync( ctx, childPage2Id ))
                .ShouldNotBeNull().ResPath.ShouldBe( $"P/{newParentWorkspaceName}/{workspaceName}/{childPage1Name}/{childPage2Name}" );
        }
    }

    [Test]
    public async Task move_sub_workspace_out_of_parent_workspace_rename_his_page_name_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<CK.DB.Workspace.Page.Package>();
        var webPageTable = services.GetRequiredService<CK.DB.WebPage.WebPageTable>();
        var zoneTable = services.GetRequiredService<CK.DB.HZone.ZoneTable>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            string parentWorkspaceName = GetNewGuid();
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, parentWorkspaceName );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, parentWorkspace.WorkspaceId );

            string workspaceName = GetNewGuid();
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, workspaceName, parentWorkspace.WorkspaceId );
            int pageId = await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );
            webPage.ShouldNotBeNull().ResPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}" );

            await zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, 0 );

            webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );
            webPage.ShouldNotBeNull().ResPath.ShouldBe( $"P/{workspaceName}" );
        }
    }

    [Test]
    public async Task set_page_to_child_workspace_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var workspacePagePkg = services.GetRequiredService<CK.DB.Workspace.Page.Package>();
        var webPageTable = services.GetRequiredService<CK.DB.WebPage.WebPageTable>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            string parentWorkspaceName = GetNewGuid();
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, parentWorkspaceName );
            await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, parentWorkspace.WorkspaceId );

            string workspaceName = GetNewGuid();
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, workspaceName, parentWorkspace.WorkspaceId );

            (await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId ))
                .ShouldBeEquivalentTo( new WorkspaceTableExtensions.WebPage { AclId = 0, PageId = 0 } );

            int pageId = await workspacePagePkg.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            var webPage = await webPageTable.GetWebPageByIdAsync( ctx, pageId );
            webPage.ShouldNotBeNull();
            webPage!.ResPath.ShouldBe( $"P/{parentWorkspaceName}/{workspaceName}" );
        }
    }

    [Test]
    public async Task cannot_move_workspace_page_as_child_of_workspace_without_page_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var workspacePageTable = services.GetRequiredService<CK.DB.Workspace.Page.Package>();
        var zoneTable = services.GetRequiredService<CK.DB.HZone.ZoneTable>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            await workspacePageTable.PlugWorkspacePageAsync( ctx, 1, workspace.WorkspaceId );

            await Util.Invokable( () => zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, parentWorkspace.WorkspaceId ) )
                           .ShouldThrowAsync<Exception>();
        }
    }

    /// <summary>
    /// Generate a new guid truncated at 32 characters.
    /// </summary>
    static string GetNewGuid( int length = 32 ) => Guid.NewGuid().ToString().Substring( 0, length );
}
