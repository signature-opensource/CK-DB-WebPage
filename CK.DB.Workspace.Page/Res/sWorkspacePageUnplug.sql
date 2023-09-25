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

    declare @PageId int;
    select @PageId = PageId
        from CK.tWorkspace
        where WorkspaceId = @WorkspaceId;

    -- If PageId is greater that 0 then the Workspace had a WebPage. Then unplug it, else no action required.
    if @PageId > 0
    begin
        --<PreUnplug />

        update CK.tWorkspace
            set PageId = 0
            where WorkspaceId = @WorkspaceId;

        exec CK.sWebPageDestroy @ActorId, @PageId, @ForceUnplug;

        --<PostUnplug />
    end

    --[endsp]
end
