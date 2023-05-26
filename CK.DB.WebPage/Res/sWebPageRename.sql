create procedure CK.sWebPageRename
(
    @ActorId int,
    @PageId int,
    @NewPageName varchar(32),
    @NewPagePath varchar(128) output
)
as
begin
    if (@ActorId is null or @ActorId <= 0) throw 50000, 'WebPage.InvalidActorId', 1;
    if (len(@NewPageName) = 0 or patindex('%[^0-9a-zA-Z-._,;]%', @NewPageName) > 0) throw 50000, 'WebPage.InvalidName', 1;

	--[beginsp]

    declare @OldPagePath varchar(128);

    select @OldPagePath = rp.ResPath
        from CK.tWebPage wp
        inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
        inner join CK.tResPath rp on wp.PageId = rp.ResId
        where PageId = @PageId and aa.GrantLevel >= 80 /* GrantLevel.SuperEditor */

    if (@OldPagePath is null) throw 50000, 'WebPage.AccessDenied', 1;

    select @NewPagePath = ParentPrefix + '/' + @NewPageName
    from CK.fResPathPrefixes(@OldPagePath)
    where ParentLevel = 1;

    if (@NewPageName is null)
    begin
        set @NewPagePath = 'P/' + @NewPageName;
    end

	--<PreRename revert />

    exec CK.sResPathRenameResPath @OldPagePath, @NewPagePath, 1;

	--<PostRename />

	--[endsp]
end
