using CK.Core;
using static CK.Testing.DBSetupTestHelper;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using CK.SqlServer;
using Dapper;
using FluentAssertions;
using CK.DB.Acl;
using CK.DB.Actor;
using CK.DB.HZone;

namespace CK.DB.HWorkspace.Tests
{
    [TestFixture]
    public class HWorkspaceTests
    {
        [Test]
        public async Task create_workspace_with_parent_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );
                var childWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString(), parentWorkspace.WorkspaceId );

                (await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                    "select ParentZoneId from CK.vZone where ZoneId = @ChildWorkspaceId;",
                    new { ChildWorkspaceId = childWorkspace.WorkspaceId } )).Should().Be( parentWorkspace.WorkspaceId );
            }
        }

        [Test]
        public async Task unplug_workspace_without_children_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );

                await workspaceTable.UnplugWorkspaceAsync( ctx, 1, workspace.WorkspaceId );

                (await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int?>(
                    "select WorkspaceId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                    new { workspace.WorkspaceId } )).Should().BeNull();
            }
        }

        [Test]
        public async Task unplug_workspace_with_children_throw_an_error_Async()
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );
                await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString(), parentWorkspace.WorkspaceId );

                await workspaceTable.Invoking(async table => await table.UnplugWorkspaceAsync( ctx, 1, parentWorkspace.WorkspaceId ) )
                                    .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task not_safe_administrator_of_workspace_cannot_move_Async()
        {
            var zoneTable = ObtainPackage<ZoneTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 127, 127, 80 /* SuperEditor */ );

                await zoneTable.Invoking( table => table.MoveZoneAsync( ctx, userId, cId, dwId ) )
                               .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task safe_administrator_of_workspace_can_move_Async()
        {
            var zoneTable = ObtainPackage<ZoneTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 127, 127, 112 /* SafeAdministrator */ );

                await zoneTable.Invoking( table => table.MoveZoneAsync( ctx, userId, cId, dwId ) )
                               .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task not_editor_of_origine_parent_workspace_cannot_move_Async()
        {
            var zoneTable = ObtainPackage<ZoneTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 32 /* Contributor */, 127, 112 );

                await zoneTable.Invoking( table => table.MoveZoneAsync( ctx, userId, cId, dwId ) )
                               .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task editor_of_origine_parent_workspace_can_move_Async()
        {
            var zoneTable = ObtainPackage<ZoneTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 64 /* Editor */, 127, 112 );

                await zoneTable.Invoking( table => table.MoveZoneAsync( ctx, userId, cId, dwId ) )
                               .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task not_editor_of_destination_parent_workspace_cannot_move_Async()
        {
            var zoneTable = ObtainPackage<ZoneTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 64, 32 /* Contributor */, 112 );

                await zoneTable.Invoking( table => table.MoveZoneAsync( ctx, userId, cId, dwId ) )
                               .Should().ThrowAsync<Exception>();
            }
        }

        [Test]
        public async Task editor_of_destination_parent_workspace_can_move_Async()
        {
            var zoneTable = ObtainPackage<ZoneTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 64, 64 /* Editor */, 112 );

                await zoneTable.Invoking( table => table.MoveZoneAsync( ctx, userId, cId, dwId ) )
                               .Should().ThrowAsync<Exception>();
            }
        }
        static async Task<(int OrigineParentId, int DestinationParentId, int ChildId, int UserId)> HiearchicalWorkspacesAsync( ISqlCallContext ctx,
                                                                                                                               byte userOrigineParentGrantLevel,
                                                                                                                               byte userDestinationParentGrantLevel,
                                                                                                                               byte userChildGrantLevel )
        {
            var workspaceTable = ObtainPackage<WorkspaceTable>();
            var userTable = ObtainPackage<UserTable>();
            var aclTable = ObtainPackage<AclTable>();
            var zoneTable = ObtainPackage<ZoneTable>();

            int userId = await userTable.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );

            var origineWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );
            await zoneTable.AddUserAsync( ctx, 1, origineWorkspace.WorkspaceId, userId );
            int aclId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                "select AclId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                new { origineWorkspace.WorkspaceId } );
            await aclTable.AclGrantSetAsync( ctx, 1, aclId, userId, "Grant user to workspace", userOrigineParentGrantLevel );

            var destinationWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );
            await zoneTable.AddUserAsync( ctx, 1, destinationWorkspace.WorkspaceId, userId );
            aclId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                "select AclId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                new { destinationWorkspace.WorkspaceId } );
            await aclTable.AclGrantSetAsync( ctx, 1, aclId, userId, "Grant user to workspace", userDestinationParentGrantLevel );

            var childWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString(), origineWorkspace.WorkspaceId );
            await zoneTable.AddUserAsync( ctx, 1, childWorkspace.WorkspaceId, userId );
            aclId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                "select AclId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                new { childWorkspace.WorkspaceId } );
            await aclTable.AclGrantSetAsync( ctx, 1, aclId, userId, "Grant user to workspace", userChildGrantLevel );

            return (origineWorkspace.WorkspaceId, destinationWorkspace.WorkspaceId, childWorkspace.WorkspaceId, userId);
        }

        static T ObtainPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} package." );
        }
    }
}
