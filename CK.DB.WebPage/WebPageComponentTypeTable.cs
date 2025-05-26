using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.WebPage;

[SqlTable( "tWebPageComponentType", Package = typeof( Package ), ResourcePath = "Res" )]
[Versions( "1.0.0" )]
public abstract class WebPageComponentTypeTable : SqlTable
{
    /// <summary>
    /// Creates a WebPageComponentType.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="typeName">The type name. Throws if name already exists.</param>
    /// <returns>The component type identifier.</returns>
    [SqlProcedure( "sWebPageComponentTypeCreate" )]
    public abstract Task<int> CreateWebPageComponentTypeAsync( ISqlCallContext ctx, int actorId, string typeName );

    /// <summary>
    /// Renames an existing WebPageComponentType.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="componentTypeId">The component type's identifier.</param>
    /// <param name="typeName">The new type name. Throws if type name already exists.</param>
    /// <returns>An awaitable</returns>
    [SqlProcedure( "sWebPageComponentTypeRename" )]
    public abstract Task RenameWebPageComponentTypeAsync( ISqlCallContext ctx, int actorId, int componentTypeId, string typeName );

    /// <summary>
    /// Destroys an existing WebPageComponentType.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="componentTypeId">The component type's identifier.</param>
    /// <param name="forceDestroy">
    /// Defines whether the target type should be destroyed even if webPages reference it.
    /// <para>When true, web pages that reference the targeted type will now reference the default componentTypeId = 0.</para>
    /// <para>When false, throws if any web page reference the targeted type.</para>
    /// </param>
    /// <returns>An awaitable</returns>
    [SqlProcedure( "sWebPageComponentTypeDestroy" )]
    public abstract Task DestroyWebPageComponentTypeAsync( ISqlCallContext ctx, int actorId, int componentTypeId, bool forceDestroy = false );
}
