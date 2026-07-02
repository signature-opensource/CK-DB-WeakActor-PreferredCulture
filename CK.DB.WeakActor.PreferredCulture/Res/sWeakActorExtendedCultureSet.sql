-- SetupConfig: {}
create procedure CK.sWeakActorExtendedCultureSet
(
    @ActorId int,
    @WeakActorId int,
    @ExtendedCultureId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @WeakActorId <= 0 throw 50000, 'WeakActor.InvalidWeakActorId', 1;
    if not exists( select 1 from CK.tCulture where CultureId = @ExtendedCultureId )
        throw 50000, 'WeakActor.InvalidExtendedCultureId', 1;

    update CK.tWeakActor
    set ExtendedCultureId = @ExtendedCultureId
    where WeakActorId = @WeakActorId;
end
