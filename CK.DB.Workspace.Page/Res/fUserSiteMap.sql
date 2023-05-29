-- SetupConfig: {}
create function CK.fUserSiteMap
(
    @ActorId int
)
returns table
as
return
(
    select w.WorkspaceId
          ,rp.ResPath
          ,aa.GrantLevel
    from CK.tWorkspace w
    inner join CK.tWebPage wp on w.PageId = wp.PageId
    inner join CK.tResPath rp on w.PageId = rp.ResId
    inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
    where aa.GrantLevel >= 16

    union -- Union workspaces page with all children of the workspaces page.

    select w.WorkspaceId
          ,rpac.ResPath
          ,aa.GrantLevel
    from CK.tWorkspace w
    inner join CK.tWebPage wp on w.PageId = wp.PageId
    inner join CK.vResPathAllChildren rpac on w.PageId = rpac.ResId
    inner join CK.vAclActor aa on wp.AclId = aa.AclId and aa.ActorId = @ActorId
    where aa.GrantLevel >= 16
);
