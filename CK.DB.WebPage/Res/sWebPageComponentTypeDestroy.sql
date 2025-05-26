create procedure CK.sWebPageComponentTypeDestroy
(
    @ActorId int,
    @ComponentTypeId int,
    @ForceDestroy bit = 0
)
as
begin
    if( @ActorId <= 0 ) throw 50000, 'WebPageComponentType.InvalidActorId', 1;
    if( @ComponentTypeId <= 0 ) throw 50000, 'WebPageComponentType.InvalidComponentTypeId', 1;

    if not exists( select 1 from CK.tWebPageComponentType where ComponentTypeId = @ComponentTypeId )
        throw 50000, 'WebPageComponentType.UnknownComponentType', 1;

    if @ForceDestroy = 0
    begin
        if exists( select 1 from CK.tWebPage where ComponentTypeId = @ComponentTypeId )
            throw 50000, 'WebPageComponentType.ComponentTypeIsReferencedByWebPages', 1;
    end
    else
    begin
        -- Reset web pages' types.
        update CK.tWebPage
        set ComponentTypeId = 0
        where ComponentTypeId = @ComponentTypeId;
    end

    delete from CK.tWebPageComponentType where ComponentTypeId = @ComponentTypeId;
end
