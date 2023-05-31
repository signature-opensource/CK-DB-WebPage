-- SetupConfig: {}
create function CK.fUserSiteMap
(
    @ActorId int
)
returns table
as
return
(
    with q
    as (
        select WorkspaceId, PageId
        from CK.tWorkspace
        where WorkspaceId > 0 and PageId > 0
    )

    select q.WorkspaceId
          ,rp.ResPath
          ,aa.GrantLevel
    from CK.tWebPage wp
    inner join CK.tResPath rp on wp.PageId = rp.ResId
    inner join q on wp.PageId = q.PageId
    inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
    where aa.GrantLevel >= 16

    union all -- Union workspaces page with all children of the workspaces page.

    select q.WorkspaceId
          ,rpac.ChildName
          ,aa.GrantLevel
    from CK.vResPathAllChildren rpac
    inner join CK.tWebPage wp on rpac.ChildId = wp.PageId
    inner join q on rpac.ResId = q.PageId
    inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
    where aa.GrantLevel >= 16
);
