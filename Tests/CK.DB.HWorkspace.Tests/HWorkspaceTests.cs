using CK.DB.Acl;
using CK.DB.Actor;
using CK.DB.HZone;
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

namespace CK.DB.HWorkspace.Tests;

[TestFixture]
public class HWorkspaceTests
{
    [Test]
    public async Task create_workspace_with_parent_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );
            var childWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString(), parentWorkspace.WorkspaceId );

            (await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                "select ParentZoneId from CK.vZone where ZoneId = @ChildWorkspaceId;",
                new { ChildWorkspaceId = childWorkspace.WorkspaceId } )).ShouldBe( parentWorkspace.WorkspaceId );
        }
    }

    [Test]
    public async Task unplug_workspace_without_children_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );

            await workspaceTable.UnplugWorkspaceAsync( ctx, 1, workspace.WorkspaceId );

            (await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int?>(
                "select WorkspaceId from CK.tWorkspace where WorkspaceId = @WorkspaceId;",
                new { workspace.WorkspaceId } )).ShouldBeNull();
        }
    }

    [Test]
    public async Task unplug_workspace_with_children_throw_an_error_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString() );
            await workspaceTable.CreateWorkspaceAsync( ctx, 1, Guid.NewGuid().ToString(), parentWorkspace.WorkspaceId );

            await Util.Awaitable( () => workspaceTable.UnplugWorkspaceAsync( ctx, 1, parentWorkspace.WorkspaceId ) )
                                .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task not_safe_administrator_of_workspace_cannot_move_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 127, 127, 80 /* SuperEditor */ );

            await Util.Invokable( () => zoneTable.MoveZoneAsync( ctx, userId, cId, dwId ) )
                           .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task safe_administrator_of_workspace_can_move_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 127, 127, 112 /* SafeAdministrator */ );

            await zoneTable.MoveZoneAsync( ctx, userId, cId, dwId );


        }
    }

    [Test]
    public async Task not_editor_of_origine_parent_workspace_cannot_move_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 32 /* Contributor */, 127, 112 );

            int parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( owId );

            await Util.Invokable( () => zoneTable.MoveZoneAsync( ctx, userId, cId, dwId ) )
                           .ShouldThrowAsync<Exception>();

            parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( owId );
        }
    }

    [Test]
    public async Task editor_of_origine_parent_workspace_can_move_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 64 /* Editor */, 127, 112 );

            int parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( owId );

            await zoneTable.MoveZoneAsync( ctx, userId, cId, dwId );

            parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( dwId );
        }
    }

    [Test]
    public async Task not_editor_of_destination_parent_workspace_cannot_move_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 64, 32 /* Contributor */, 112 );

            int parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( owId );

            await Util.Invokable( () => zoneTable.MoveZoneAsync( ctx, userId, cId, dwId ) )
                           .ShouldThrowAsync<Exception>();

            parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( owId );
        }
    }

    [Test]
    public async Task editor_of_destination_parent_workspace_can_move_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var (owId, dwId, cId, userId) = await HiearchicalWorkspacesAsync( ctx, 64, 64 /* Editor */, 112 );

            int parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( owId );

            await zoneTable.MoveZoneAsync( ctx, userId, cId, dwId );

            parentZoneId = await ctx.GetConnectionController( zoneTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId;",
                new { WorkspaceId = cId } );
            parentZoneId.ShouldBe( dwId );
        }
    }

    [Test]
    public async Task cannot_move_workspace_in_Zone_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var zoneTable = services.GetRequiredService<ZoneTable>();
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid() );
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, 1, GetNewGuid(), parentWorkspace.WorkspaceId );

            int parentId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                @"select ZoneId from CK.vGroup where GroupId = @WorkspaceId;",
                new { workspace.WorkspaceId } );

            parentId.ShouldBe( parentWorkspace.WorkspaceId );

            var parentZoneId = await zoneTable.CreateZoneAsync( ctx, 1 );

            await Util.Invokable( () => zoneTable.MoveZoneAsync( ctx, 1, workspace.WorkspaceId, parentZoneId ) )
                           .ShouldThrowAsync<Exception>();

            parentId = await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                @"select ZoneId from CK.vGroup where GroupId = @WorkspaceId;",
                new { workspace.WorkspaceId } );

            parentId.ShouldBe( parentWorkspace.WorkspaceId );
        }
    }

    static async Task<(int OrigineParentId, int DestinationParentId, int ChildId, int UserId)> HiearchicalWorkspacesAsync( ISqlCallContext ctx,
                                                                                                                           byte userOrigineParentGrantLevel,
                                                                                                                           byte userDestinationParentGrantLevel,
                                                                                                                           byte userChildGrantLevel )
    {
        var services = SharedEngine.AutomaticServices;
        var workspaceTable = services.GetRequiredService<WorkspaceTable>();
        var userTable = services.GetRequiredService<UserTable>();
        var aclTable = services.GetRequiredService<AclTable>();
        var zoneTable = services.GetRequiredService<ZoneTable>();

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

    [Test]
    public async Task simple_workspace_admin_cannot_move_sub_workspace_to_parent_workspace_0_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var userTable = services.GetRequiredService<CK.DB.Actor.UserTable>();
        var aclTable = services.GetRequiredService<CK.DB.Acl.AclTable>();
        var workspaceTable = services.GetRequiredService<CK.DB.HWorkspace.WorkspaceTable>();
        var zoneTable = services.GetRequiredService<CK.DB.HZone.ZoneTable>();

        using( SqlStandardCallContext ctx = new( TestHelper.Monitor ) )
        {
            int userId = await userTable.CreateUserAsync( ctx, 1, GetNewGuid() );
            await aclTable.AclGrantSetAsync( ctx, 1, 1, userId, "Safe Administrator", 112 );
            var parentWorkspace = await workspaceTable.CreateWorkspaceAsync( ctx, userId, GetNewGuid() );
            var workspace = await workspaceTable.CreateWorkspaceAsync( ctx, userId, GetNewGuid(), parentWorkspace.WorkspaceId );

            await Util.Invokable( () => zoneTable.MoveZoneAsync( ctx, userId, workspace.WorkspaceId, 0 ) )
                .ShouldThrowAsync<Exception>();

            (await ctx.GetConnectionController( workspaceTable ).QuerySingleOrDefaultAsync<int>(
                @"select ParentZoneId from CK.vZone where ZoneId = @WorkspaceId",
                new { workspace.WorkspaceId } ))
                .ShouldBe( parentWorkspace.WorkspaceId );
        }
    }

    static string GetNewGuid( int length = 32 ) => Guid.NewGuid().ToString().Substring( 0, length );
}
