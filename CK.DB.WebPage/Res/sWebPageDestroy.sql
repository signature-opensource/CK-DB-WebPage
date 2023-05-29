-- SetupConfig: {}
create procedure CK.sWebPageDestroy
(
    @ActorId int,
    @PageId int,
    @WithChildren bit = 0
)
as
begin
    if (@ActorId is null or @ActorId <= 0) throw 50000, 'WebPage.InvalidActorId', 1;

	--[beginsp]

    if (not exists (
        select 1
        from CK.tWebPage wp
        inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
        where aa.GrantLevel >= 127 /* Administrator */
    ))
    begin
        ;throw 50000, 'WebPage.AccessDenied', 1;
    end

    if @WithChildren = 1
    begin
        declare @ChildId int;
        declare CChildren cursor local static forward_only read_only for
            select ChildId
            from CK.vResPathAllChildren
            where ResId = @PageId
            order by len(ChildName) desc;

        open CChildren;
        fetch next from CChildren into @ChildId;
        while @@fetch_status = 0
        begin
            exec CK.sWebPageDestroy @ActorId, @ChildId; -- no need to call recursive destroy
            fetch next from CChildren into @ChildId;
        end
        close CChildren;
        deallocate CChildren;
    end

	--<PreDestroy revert />

    delete from CK.tWebPage where PageId = @PageId;
    exec CK.sResPathDestroy @PageId;
    -- Should remove Acl?
	
	--<PostDestroy />	
	
	--[endsp]
end
