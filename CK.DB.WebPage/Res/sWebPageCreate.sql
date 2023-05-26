-- SetupConfig: {}
create procedure CK.sWebPageCreate
(
    @ActorId int,
    @ParentPageId int,
    @PageName varchar(32),
    @AclId int = 0,
    @PageId int output
)
as
begin
    if (@ActorId is null or @ActorId <= 0) throw 50000, 'WebPage.InvalidActorId', 1;
    if (@ParentPageId is null or @ParentPageId < 0 ) throw 50000, 'WebPage.InvalidParentId', 1;
    if (len(@PageName) = 0 or patindex('%[^0-9a-zA-Z-._,;]%', @PageName) > 0) throw 50000, 'WebPage.InvalidName', 1;

	--[beginsp]

    declare @PagePath varchar(128);
    declare @ParentAclId int;

    if (@ParentPageId = 0)
    begin
        set @PagePath = 'P/' + @PageName;
        set @ParentAclId = 0;
    end
    else
    begin
        -- If ResPath + '/' + PageName over 128 character, the PagePath will be truncated.
        select @PagePath = rp.ResPath + '/' + @PageName,
               @ParentAclId = wp.AclId
            from CK.tResPath rp
            inner join CK.tWebPage wp on rp.ResId = wp.PageId
            inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
            where rp.ResId = @ParentPageId and aa.GrantLevel >= 32;

        if (@PagePath is null) throw 50000, 'WebPage.InvalidParentId', 1;
    end

    if (@AclId = 0)
    begin
        if (@ParentPageId = 0)
        begin
           exec CK.sAclCreate @ActorId, @AclId output;
           exec CK.sAclGrantSet @ActorId, @AclId, @ActorId, 'Creator', 127;
        end
        else
        begin
            set @AclId = @ParentAclId;
        end
    end

	--<PreCreate revert />

    exec CK.sResCreateWithResPath @PagePath, @PageId output;

    insert into CK.tWebPage ( PageId, AclId ) values( @PageId, @AclId );
	
	--<PostCreate />	
	
	--[endsp]
end
