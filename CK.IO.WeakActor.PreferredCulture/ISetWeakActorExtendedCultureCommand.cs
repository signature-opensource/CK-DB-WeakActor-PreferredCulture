using CK.Auth;
using CK.Cris;

namespace CK.IO.WeakActor.PreferredCulture;

/// <summary>
/// Sets the preferred extended culture of a WeakActor.
/// </summary>
public interface ISetWeakActorExtendedCultureCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    /// <summary>
    /// Gets or sets the WeakActor identifier.
    /// </summary>
    public int WeakActorId { get; set; }

    /// <summary>
    /// Gets or sets the preferred culture: must exist in CK.tCulture.
    /// </summary>
    public int ExtendedCultureId { get; set; }
}
