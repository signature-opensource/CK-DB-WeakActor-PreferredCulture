Adds a preferred culture to the weak actors: the `ExtendedCultureId` column of `CK.tWeakActor`, a culture of
CK.DB.Globalization set at creation or later, and exposed by `CK.vWeakActor`.

The `Package` creates a weak actor with its culture (`Package.CreateWeakActorAsync`) and changes it
(`Package.SetExtendedCultureAsync`); it also handles the `ISetWeakActorExtendedCultureCommand` of
CK.IO.WeakActor.PreferredCulture.
