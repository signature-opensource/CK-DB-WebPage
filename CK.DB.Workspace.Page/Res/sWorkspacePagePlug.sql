-- SetupConfig: {}
create procedure CK.sWorkspacePagePlug
(
    @ActorId int,
    @WorkspaceId int
)
as
begin
    --[beginsp]

    declare @WorkspaceName nvarchar(128);
    declare @WorkspaceAlcId int;

    select @WorkspaceName = WorkspaceName, @WorkspaceAlcId = AclId
        from CK.vWorkspace
        where WorkspaceId = @WorkspaceId;

    if (len(@WorkspaceName) > 32) throw 50000, 'WorkspaceNameIsTooLong', 1;

    --<PrePlug revert>

    declare @PageId int;
    exec CK.sWebPageCreate @ActorId, 0, @WorkspaceName, @WorkspaceAlcId, @PageId output;

    update CK.tWorkspace
        set PageId = @PageId
        where WorkspaceId = @WorkspaceId;

    --<PostPlug />

    --[endsp]
end
