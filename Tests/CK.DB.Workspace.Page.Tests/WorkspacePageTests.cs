using CK.Core;
using CK.DB.Acl;
using CK.DB.Actor;
using CK.DB.WebPage;
using CK.SqlServer;
using static CK.Testing.DBSetupTestHelper;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CK.DB.WebPage.Tests;

namespace CK.DB.Workspace.Page.Tests
{
    [TestFixture]
    public class WorkspacePageTests
    {
        [Test]
        public async Task create_workspace_creates_webPage_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

                var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );

                webPage.Should().NotBeNull();
                webPage!.PageId.Should().BeGreaterThan( 0 );
            }
        }

        [Test]
        public async Task workspace_page_have_same_alc_that_workspace_Async()
        {
            //TODO: Check when the CK.DB.Workspace PreCreate fix (commit 31a737f) will be released.

            var workspaceTable = ObtainPackage<WorkspaceTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
                int workspaceAclId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                    @"select AclId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                    new { workspace.WorkspaceId } );

                (await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId ))!.AclId.Should().Be( workspaceAclId );
            }
        }

        [Test]
        public async Task destroy_workspace_destroy_all_webPages_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();
            var webPageTable = ObtainPackage<WebPageTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

                // Create children webPage
                var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
                List<int> children = new()
                {
                    await webPageTable.CreateWebPageAsync( ctx, 1, webPage!.PageId, GetNewGuid() ),
                    await webPageTable.CreateWebPageAsync( ctx, 1, webPage.PageId, GetNewGuid() ),
                };
                children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[0], GetNewGuid() ) );
                children.Add( await webPageTable.CreateWebPageAsync( ctx, 1, children[2], GetNewGuid() ) );
                int otherPageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, GetNewGuid() );

                // Check if children exists
                foreach( int childId in children )
                {
                    (await webPageTable.GetWebPageByIdAsync( ctx, childId )).Should().NotBeNull();
                }
                (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).Should().NotBeNull();

                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 1 )).Should().HaveCount( 5 );

                // Destroy workspace
                await workspaceTable.DestroyWorkspaceAsync( ctx, 1, workspace.WorkspaceId );

                // Check if children exists
                (await webPageTable.GetWebPageByIdAsync( ctx, webPage.PageId )).Should().BeNull();
                foreach( int childId in children )
                {
                    (await webPageTable.GetWebPageByIdAsync( ctx, childId )).Should().BeNull();
                }
                (await webPageTable.GetWebPageByIdAsync( ctx, otherPageId )).Should().NotBeNull();

                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 1 )).Should().BeEmpty();
            }
        }

        [Test]
        public async Task invalid_user_cannot_obtain_workspace_siteMap_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();
            var userTable = ObtainPackage<UserTable>();
            var aclTable = ObtainPackage<AclTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

                var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
                webPage.Should().NotBeNull();

                // Anonimous user
                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 0 )).Should().BeEmpty();


                // Not workspace member
                int userId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );
                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).Should().BeEmpty();

                // Not viewer
                await aclTable.AclGrantSetAsync( ctx, 1, webPage!.AclId, userId, "User", 8 );
                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).Should().BeEmpty();

                // Viewer
                await aclTable.AclGrantSetAsync( ctx, 1, webPage!.AclId, userId, "Viewer", 16 );
                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).Should().NotBeEmpty().And.HaveCount( 1 );
            }
        }

        [Test]
        public async Task workspace_viewer_can_obtain_siteMap_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();
            var userTable = ObtainPackage<UserTable>();
            var aclTalbe = ObtainPackage<AclTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

                int userId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );
                var webPage = await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId );
                await aclTalbe.AclGrantSetAsync( ctx, 1, webPage!.AclId, userId, "Viewer", 16 );

                (await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, userId )).Should().NotBeEmpty().And.HaveCount( 1 );
            }
        }

        [Test]
        public async Task get_workspace_webPage_and_children_pages_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();
            var webPageTable = ObtainPackage<WebPageTable>();
            var userTable = ObtainPackage<UserTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );

                List<int> webPageIds = new()
                {
                    (await workspaceTable.GetWebPageFromWorkspaceIdAsync( ctx, workspace.WorkspaceId ))!.PageId
                };

                webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[0], GetNewGuid() ) );
                webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[0], GetNewGuid() ) );
                webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[2], GetNewGuid() ) );
                webPageIds.Add( await webPageTable.CreateWebPageAsync( ctx, 1, webPageIds[3], GetNewGuid() ) );

                var sitemap = await workspaceTable.GetWorkspaceSiteMapAsync( ctx, workspace.WorkspaceId, 1 );
                sitemap.Should().HaveCount( webPageIds.Count );

                foreach( var webPageId in webPageIds )
                {
                    sitemap.Should().Contain( siteMapitem => siteMapitem.PageId == webPageId );
                }
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
