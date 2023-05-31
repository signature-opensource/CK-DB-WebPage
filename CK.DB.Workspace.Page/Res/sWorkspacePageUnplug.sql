-- SetupConfig: {}
create procedure CK.sWorkspacePageUnplug
(
    @ActorId int,
    @WorkspaceId int,
    @ForceUnplug bit = 0
)
as
begin
    --[beginsp]

    --<PreUnplug />

    declare @PageId int;
    select @PageId = PageId
        from CK.tWorkspace
        where WorkspaceId = @WorkspaceId;

    update CK.tWorkspace
        set PageId = 0
        where WorkspaceId = @WorkspaceId;

    exec CK.sWebPageDestroy @ActorId, @PageId, @ForceUnplug;

    --<PostUnplug />

    --[endsp]
end
