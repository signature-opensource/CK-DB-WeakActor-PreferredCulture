using CK.Core;
using CK.Cris;
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
public class WeakActorExtendedCultureCommandTests
{
    // Value of the DF_CK_tWeakActor_ExtendedCultureId default constraint (the "en" culture seeded by CK.DB.Globalization).
    const int DefaultExtendedCultureId = 221272233;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    AsyncServiceScope _scope;
    CrisExecutionContext _executor;
    PocoDirectory _pocoDir;
    WeakActorTable _weakActorTable;
#pragma warning restore CS8618

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _scope = SharedEngine.AutomaticServices.CreateAsyncScope();
        var services = _scope.ServiceProvider;

        _pocoDir = services.GetRequiredService<PocoDirectory>();
        _executor = services.GetRequiredService<CrisExecutionContext>();
        _weakActorTable = services.GetRequiredService<WeakActorTable>();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _scope.DisposeAsync();
    }

    [Test]
    public async Task can_set_weakActor_extendedCulture_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var weakActorId = await _weakActorTable.CreateAsync( ctx, 1, Guid.NewGuid().ToString() );
        var otherCultureId = PickAnotherCultureId( ctx );

        var cmd = _pocoDir.Create<IO.WeakActor.PreferredCulture.ISetWeakActorExtendedCultureCommand>( c =>
        {
            c.ActorId = 1;
            c.WeakActorId = weakActorId;
            c.ExtendedCultureId = otherCultureId;
        } );
        var execCmd = await _executor.ExecuteRootCommandAsync( cmd );
        var res = execCmd.WithResult<ICrisBasicCommandResult>().Result;
        res.UserMessages.ShouldNotBeNull();

        ctx[_weakActorTable].QuerySingle<int>(
            "select ExtendedCultureId from CK.tWeakActor where WeakActorId = @WeakActorId;",
            new { WeakActorId = weakActorId } )
            .ShouldBe( otherCultureId );
    }

    // Picks any valid culture other than the default one.
    int PickAnotherCultureId( ISqlCallContext ctx )
        => ctx[_weakActorTable].QuerySingle<int>(
            "select top 1 CultureId from CK.tCulture where CultureId <> @Cur and CultureId <> 0 order by CultureId;",
            new { Cur = DefaultExtendedCultureId } );
}
