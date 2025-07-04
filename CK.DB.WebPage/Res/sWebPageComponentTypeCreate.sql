create procedure CK.sWebPageComponentTypeCreate
(
    @ActorId int,
    @TypeName varchar( 128 ),
    @IdResult int output
)
as
begin
    if( @ActorId <= 0 ) throw 50000, 'WebPageComponentType.InvalidActorId', 1;
    if( len( @TypeName ) = 0 ) throw 50000, 'WebPageComponentType.InvalidTypeName', 1;

    if exists( select 1 from CK.tWebPageComponentType where TypeName = @TypeName )
        throw 50000, 'WebPageComponentType.TypeNameAlreadyExists', 1;

    insert into CK.tWebPageComponentType( TypeName ) values( @TypeName );
    set @IdResult = scope_identity();
end
