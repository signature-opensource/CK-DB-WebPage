-- SetupConfig: {}
create view CK.vWebPage
as
    select wp.PageId
          ,wp.AclId
          ,rp.ResPath
          ,rs.[Value] as PageTitle
    from CK.tWebPage wp
    inner join CK.tResPath rp on wp.PageId = rp.ResId
    inner join CK.tResString rs on wp.PageId = rs.ResId;
