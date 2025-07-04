using Azure;
using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace CK.DB.WebPage.Tests;

[TestFixture]
public class WebPageComponentTypeTests
{
    [TestCase( -3712 )]
    [TestCase( 0 )]
    public async Task invalid_actor_cannot_create_component_type_Async( int actorId )
    {
        var services = SharedEngine.AutomaticServices;
        var table = services.GetRequiredService<WebPageComponentTypeTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            await Util.Awaitable( () => table.CreateWebPageComponentTypeAsync( ctx, actorId, Guid.NewGuid().ToString() ) )
                .ShouldThrowAsync<Exception>();
        }
    }

    [Test]
    public async Task can_create_component_type_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var table = services.GetRequiredService<WebPageComponentTypeTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            await Util.Awaitable( () => table.CreateWebPageComponentTypeAsync( ctx, 1, Guid.NewGuid().ToString() ) )
                .ShouldNotThrowAsync();
        }
    }

    [Test]
    public async Task can_rename_component_type_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var table = services.GetRequiredService<WebPageComponentTypeTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var typeId = await table.CreateWebPageComponentTypeAsync( ctx, 1, Guid.NewGuid().ToString() );
            var typeName = await ctx.GetConnectionController( table ).QuerySingleOrDefaultAsync<string>(
                "select TypeName from CK.tWebPageComponentType where ComponentTypeId = @TypeId",
                new { TypeId = typeId } );

            var newName = Guid.NewGuid().ToString();
            await Util.Awaitable( () => table.RenameWebPageComponentTypeAsync( ctx, 1, typeId, newName ) )
                .ShouldNotThrowAsync();

            typeName = await ctx.GetConnectionController( table ).QuerySingleOrDefaultAsync<string>(
                "select TypeName from CK.tWebPageComponentType where ComponentTypeId = @TypeId",
                new { TypeId = typeId } );
            typeName.ShouldBe( newName );
        }
    }

    [Test]
    public async Task can_destroy_component_type_Async()
    {
        var services = SharedEngine.AutomaticServices;
        var table = services.GetRequiredService<WebPageComponentTypeTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var typeId = await table.CreateWebPageComponentTypeAsync( ctx, 1, Guid.NewGuid().ToString() );
            await Util.Awaitable( () => table.DestroyWebPageComponentTypeAsync( ctx, 1, typeId ) )
                .ShouldNotThrowAsync();
        }
    }

    [TestCase( true )]
    [TestCase( false )]
    public async Task can_destroy_component_type_Async( bool forceDestroy )
    {
        var services = SharedEngine.AutomaticServices;
        var table = services.GetRequiredService<WebPageComponentTypeTable>();
        var webPageTable = services.GetRequiredService<WebPageTable>();

        using( SqlStandardCallContext ctx = new() )
        {
            var typeId = await table.CreateWebPageComponentTypeAsync( ctx, 1, Guid.NewGuid().ToString() );
            string pageName = Guid.NewGuid().ToString();
            int pageId = await webPageTable.CreateWebPageAsync( ctx, 1, 0, pageName, pageName, componentTypeId: typeId );

            if( forceDestroy )
            {
                await Util.Awaitable( () => table.DestroyWebPageComponentTypeAsync( ctx, 1, typeId, forceDestroy ) )
                    .ShouldNotThrowAsync();
            }
            else
            {
                await Util.Awaitable( () => table.DestroyWebPageComponentTypeAsync( ctx, 1, typeId, forceDestroy ) )
                    .ShouldThrowAsync<SqlDetailedException>();
            }
        }
    }
}
