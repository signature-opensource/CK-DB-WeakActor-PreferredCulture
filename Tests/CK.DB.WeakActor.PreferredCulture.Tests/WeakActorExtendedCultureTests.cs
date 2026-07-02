using CK.Core;
using CK.DB.Actor.WeakActor;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.WeakActor.PreferredCulture.Tests;

[TestFixture]
public class WeakActorExtendedCultureTests
{
    // Value of the DF_CK_tWeakActor_ExtendedCultureId default constraint (the "en" culture seeded by CK.DB.Globalization).
    const int DefaultExtendedCultureId = 221272233;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    AsyncServiceScope _scope;
    Package _package;
    WeakActorTable _weakActorTable;
#pragma warning restore CS8618

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _scope = SharedEngine.AutomaticServices.CreateAsyncScope();
        var services = _scope.ServiceProvider;
        _package = services.GetRequiredService<Package>();
        _weakActorTable = services.GetRequiredService<WeakActorTable>();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _scope.DisposeAsync();
    }

    [Test]
    public async Task new_weakActor_has_the_default_extended_culture_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var weakActorId = await _weakActorTable.CreateAsync( ctx, 1, Guid.NewGuid().ToString() );

        ReadExtendedCultureId( ctx, weakActorId ).ShouldBe( DefaultExtendedCultureId );
    }

    [Test]
    public async Task create_weakActor_with_an_explicit_extended_culture_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var otherCultureId = PickAnotherCultureId( ctx );

        var weakActorId = await _package.CreateWeakActorAsync( ctx, 1, Guid.NewGuid().ToString(), otherCultureId );

        ReadExtendedCultureId( ctx, weakActorId ).ShouldBe( otherCultureId );
    }

    [Test]
    public async Task set_extended_culture_updates_the_weakActor_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var weakActorId = await _weakActorTable.CreateAsync( ctx, 1, Guid.NewGuid().ToString() );
        var otherCultureId = PickAnotherCultureId( ctx );

        await _package.SetExtendedCultureAsync( ctx, 1, weakActorId, otherCultureId );

        ReadExtendedCultureId( ctx, weakActorId ).ShouldBe( otherCultureId );
    }

    [Test]
    public async Task set_extended_culture_to_an_unknown_culture_throws_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var weakActorId = await _weakActorTable.CreateAsync( ctx, 1, Guid.NewGuid().ToString() );

        // -1 cannot exist in CK.tCulture.
        await Util.Invokable( () => _package.SetExtendedCultureAsync( ctx, 1, weakActorId, -1 ) )
                  .ShouldThrowAsync<SqlDetailedException>();

        // The WeakActor has not been touched.
        ReadExtendedCultureId( ctx, weakActorId ).ShouldBe( DefaultExtendedCultureId );
    }

    [Test]
    public async Task anonymous_cannot_set_extended_culture_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var weakActorId = await _weakActorTable.CreateAsync( ctx, 1, Guid.NewGuid().ToString() );

        await Util.Invokable( () => _package.SetExtendedCultureAsync( ctx, 0, weakActorId, DefaultExtendedCultureId ) )
                  .ShouldThrowAsync<SqlDetailedException>();
    }

    [Test]
    public async Task set_extended_culture_with_invalid_weakActorId_throws_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );

        await Util.Invokable( () => _package.SetExtendedCultureAsync( ctx, 1, 0, DefaultExtendedCultureId ) )
                  .ShouldThrowAsync<SqlDetailedException>();
    }

    int ReadExtendedCultureId( ISqlCallContext ctx, int weakActorId )
        => ctx[_weakActorTable].QuerySingle<int>(
            "select ExtendedCultureId from CK.tWeakActor where WeakActorId = @WeakActorId;",
            new { WeakActorId = weakActorId } );

    // Picks any valid culture other than the default one.
    int PickAnotherCultureId( ISqlCallContext ctx )
        => ctx[_weakActorTable].QuerySingle<int>(
            "select top 1 CultureId from CK.tCulture where CultureId <> @Cur and CultureId <> 0 order by CultureId;",
            new { Cur = DefaultExtendedCultureId } );
}
