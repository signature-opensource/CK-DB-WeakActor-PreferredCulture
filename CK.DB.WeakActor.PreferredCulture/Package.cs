using CK.Core;
using CK.Cris;
using CK.IO.WeakActor.PreferredCulture;
using CK.SqlServer;
using System.Diagnostics.CodeAnalysis;

namespace CK.DB.WeakActor.PreferredCulture;

/// <summary>
/// Adds a preferred <c>ExtendedCultureId</c> (foreign key to <c>CK.tCulture</c>) to WeakActors.
/// </summary>
[SqlPackage( FullName = "CK.WeakActor.PreferredCulture.Package", Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:vWeakActor" )]
public abstract partial class Package : SqlPackage
{
    [AllowNull]
    Actor.WeakActor.Package _weakActorPackage;

    [AllowNull]
    Globalization.CultureTable _cultureTable;

    void StObjConstruct( Actor.WeakActor.Package weakActorPackage, Globalization.CultureTable cultureTable )
    {
        _weakActorPackage = weakActorPackage;
        _cultureTable = cultureTable;
    }

    /// <summary>
    /// Creates a WeakActor with an explicit preferred extended culture.
    /// </summary>
    /// <param name="ctx">The SQL call context.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="weakActorName">The WeakActor name to create.</param>
    /// <param name="extendedCultureId">The preferred culture: must exist in CK.tCulture.</param>
    /// <returns>The WeakActor identifier.</returns>
    [SqlProcedure( "transform:sWeakActorCreate" )]
    public abstract Task<int> CreateWeakActorAsync( ISqlCallContext ctx,
                                                    int actorId,
                                                    string weakActorName,
                                                    int extendedCultureId );

    /// <summary>
    /// Sets the preferred extended culture of a WeakActor.
    /// </summary>
    /// <param name="ctx">The SQL call context.</param>
    /// <param name="actorId">The acting actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier.</param>
    /// <param name="extendedCultureId">The preferred culture: must exist in CK.tCulture.</param>
    /// <returns>An awaitable.</returns>
    [SqlProcedure( "CK.sWeakActorExtendedCultureSet" )]
    public abstract Task SetExtendedCultureAsync( ISqlCallContext ctx, int actorId, int weakActorId, int extendedCultureId );

    /// <summary>
    /// Handles <see cref="ISetWeakActorExtendedCultureCommand"/>.
    /// </summary>
    /// <param name="ctx">The SQL call context.</param>
    /// <param name="command">The command.</param>
    /// <returns>The command result.</returns>
    [CommandHandler]
    [SqlProcedure( "CK.sWeakActorExtendedCultureSet" )]
    public abstract Task<ICrisBasicCommandResult> SetExtendedCultureAsync( ISqlCallContext ctx, [ParameterSource] ISetWeakActorExtendedCultureCommand command );
}
