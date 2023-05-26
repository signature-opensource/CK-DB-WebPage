-- SetupConfig: {}
create procedure CK.sWebPageDestroy
(
    @ActorId int,
    @PageId int
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

	--<PreDestroy revert />
    
    delete from CK.tWebPage where PageId = @PageId;
    exec CK.sResPathDestroy @PageId;

    -- Should remove Acl?
	
	--<PostDestroy />	
	
	--[endsp]
end
