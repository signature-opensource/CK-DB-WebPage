create procedure CK.sWebPageComponentTypeRename
(
    @ActorId int,
    @ComponentTypeId int,
    @TypeName varchar( 128 )
)
as
begin
    if( @ActorId <= 0 ) throw 50000, 'WebPageComponentType.InvalidActorId', 1;
    if( @ComponentTypeId <= 0 ) throw 50000, 'WebPageComponentType.InvalidComponentTypeId', 1;
    if( len( @TypeName ) = 0 ) throw 50000, 'WebPageComponentType.InvalidTypeName', 1;

    if not exists( select 1 from CK.tWebPageComponentType where ComponentTypeId = @ComponentTypeId )
        throw 50000, 'WebPageComponentType.UnknownComponentType', 1;

    if exists( select 1 from CK.tWebPageComponentType where ComponentTypeId <> @ComponentTypeId and TypeName = @TypeName )
        throw 50000, 'WebPageComponentType.TypeNameAlreadyExists', 1;

    update CK.tWebPageComponentType
    set TypeName = @TypeName
    where ComponentTypeId = @ComponentTypeId;
end
