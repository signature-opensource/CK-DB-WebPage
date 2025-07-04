-- SetupConfig: {}
create procedure CK.sWorkspacePagePlug
(
    @ActorId int,
    @WorkspaceId int,
    @PageId int output
)
as
begin
    --[beginsp]

    if @WorkspaceId <= 0 throw 50000, 'Workspace.Page.InvalidWorkspaceId', 1;

    -- Check the current PageId of the workspace
    select @PageId = PageId
        from CK.tWorkspace
        where WorkspaceId = @WorkspaceId;

    if @PageId is null throw 50000, 'Workspace.Page.InvalidWorkspaceId', 1;

    -- Create a WebPage and plug it to the Workspace only if it has no page (tWorkspace.PageId = 0)
    if @PageId = 0
    begin
        declare @WorkspaceName nvarchar(128);
        declare @WorkspaceAclId int;
    
        select @WorkspaceName = WorkspaceName, @WorkspaceAclId = AclId
            from CK.vWorkspace
            where WorkspaceId = @WorkspaceId;
    
        if (len(@WorkspaceName) > 32) throw 50000, 'WorkspaceNameIsTooLong', 1;
    
        --<PrePlug revert>

        declare @ComponentTypeId int = 0;
        exec CK.sWebPageCreate @ActorId, 0, @WorkspaceName, @WorkspaceName, @WorkspaceAclId, @ComponentTypeId, @PageId output;
    
        update CK.tWorkspace
            set PageId = @PageId
            where WorkspaceId = @WorkspaceId;
    
        --<PostPlug />
    end
    --[endsp]
end
