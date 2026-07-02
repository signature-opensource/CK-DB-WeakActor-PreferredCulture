using CK.Core;
using CK.DB.Actor.WeakActor;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System.Reflection;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.WeakActor.PreferredCulture.Tests;

/// <summary>
/// Tests the handover from the legacy PreferredLCID column (an int without foreign key that referenced
/// the former CK.tLCID table and was therefore missed by the CK.DB.Globalization bazooka).
/// The real embedded Install.1.0.0 script is executed against a simulated legacy schema: this works
/// because the script is idempotent (guarded by column existence checks).
/// </summary>
[TestFixture]
[NonParallelizable]
public class LegacyPreferredLCIDMigrationTests
{
    // Value of the DF_CK_tWeakActor_ExtendedCultureId default constraint (the "en" culture seeded by CK.DB.Globalization).
    const int DefaultExtendedCultureId = 221272233;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    AsyncServiceScope _scope;
    WeakActorTable _weakActorTable;
#pragma warning restore CS8618

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _scope = SharedEngine.AutomaticServices.CreateAsyncScope();
        _weakActorTable = _scope.ServiceProvider.GetRequiredService<WeakActorTable>();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _scope.DisposeAsync();
    }

    [Test]
    public async Task legacy_PreferredLCID_values_are_migrated_and_the_column_is_dropped_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );

        // Reads the script upfront so that no failure can occur between the schema mutation below
        // and its replay (that restores the nominal schema).
        var installScript = ReadInstallScript();

        // Known LCIDs, plus 99 that is unknown to the mapping and must fall back to the default culture.
        var weakActors = new Dictionary<int, int>();
        foreach( var lcid in new[] { 12, 1036, 9, 2057, 99 } )
        {
            weakActors[lcid] = await _weakActorTable.CreateAsync( ctx, 1, Guid.NewGuid().ToString() );
        }

        // Simulates the legacy schema: no ExtendedCultureId, a PreferredLCID without foreign key
        // (same shape as the former Promod Operator column). Existence guards make this replayable
        // from any state (a previously failed run may have left the legacy schema in place).
        ctx[_weakActorTable].Execute( @"
            if exists( select * from sys.foreign_keys where name = 'FK_CK_tWeakActor_ExtendedCultureId' )
                alter table CK.tWeakActor drop constraint FK_CK_tWeakActor_ExtendedCultureId;
            if exists( select * from sys.default_constraints where name = 'DF_CK_tWeakActor_ExtendedCultureId' )
                alter table CK.tWeakActor drop constraint DF_CK_tWeakActor_ExtendedCultureId;
            if exists( select * from sys.columns where object_id = object_id( N'CK.tWeakActor' ) and name = 'ExtendedCultureId' )
                alter table CK.tWeakActor drop column ExtendedCultureId;
            if not exists( select * from sys.columns where object_id = object_id( N'CK.tWeakActor' ) and name = 'PreferredLCID' )
                alter table CK.tWeakActor add PreferredLCID int not null constraint DF_tWeakActor_PreferredLCID default( 9 );" );
        foreach( var (lcid, weakActorId) in weakActors )
        {
            ctx[_weakActorTable].Execute(
                "update CK.tWeakActor set PreferredLCID = @LCID where WeakActorId = @WeakActorId;",
                new { LCID = lcid, WeakActorId = weakActorId } );
        }

        // Replays the real embedded install script.
        ctx[_weakActorTable].Execute( installScript );

        // Values migrated according to the legacy LCID -> CultureId bazooka mapping.
        ReadExtendedCultureId( ctx, weakActors[12] ).ShouldBe( 210327884 );    // fr
        ReadExtendedCultureId( ctx, weakActors[1036] ).ShouldBe( 1629332867 ); // fr-fr
        ReadExtendedCultureId( ctx, weakActors[9] ).ShouldBe( 221272233 );     // en
        ReadExtendedCultureId( ctx, weakActors[2057] ).ShouldBe( 926936865 );  // en-gb
        ReadExtendedCultureId( ctx, weakActors[99] ).ShouldBe( DefaultExtendedCultureId );

        // The legacy column is gone, the new column and its constraints are back.
        ColumnExists( ctx, "PreferredLCID" ).ShouldBeFalse();
        ColumnExists( ctx, "ExtendedCultureId" ).ShouldBeTrue();
        ctx[_weakActorTable].QuerySingle<int>(
            "select count(*) from sys.foreign_keys where name = 'FK_CK_tWeakActor_ExtendedCultureId';" )
            .ShouldBe( 1 );
        ctx[_weakActorTable].QuerySingle<int>(
            "select count(*) from sys.default_constraints where name = 'DF_CK_tWeakActor_ExtendedCultureId';" )
            .ShouldBe( 1 );
    }

    static string ReadInstallScript()
    {
        var assembly = typeof( Package ).Assembly;
        // The engine also embeds a 'ck@Res/...' copy of each resource: skips it.
        var resourceName = assembly.GetManifestResourceNames()
                                   .Single( n => n.EndsWith( "Model.CK.WeakActor.PreferredCulture.Package.Install.1.0.0.sql" )
                                                 && !n.StartsWith( "ck@" ) );
        using var stream = assembly.GetManifestResourceStream( resourceName )!;
        using var reader = new StreamReader( stream );
        var script = reader.ReadToEnd();
        return script.Replace( "--[beginscript]", "" ).Replace( "--[endscript]", "" );
    }

    int ReadExtendedCultureId( ISqlCallContext ctx, int weakActorId )
        => ctx[_weakActorTable].QuerySingle<int>(
            "select ExtendedCultureId from CK.tWeakActor where WeakActorId = @WeakActorId;",
            new { WeakActorId = weakActorId } );

    bool ColumnExists( ISqlCallContext ctx, string columnName )
        => ctx[_weakActorTable].QuerySingle<int>(
            "select count(*) from sys.columns where object_id = object_id( N'CK.tWeakActor' ) and name = @ColumnName;",
            new { ColumnName = columnName } ) == 1;
}
