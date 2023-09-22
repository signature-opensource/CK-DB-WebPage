-- SetupConfig: {}
create view CK.vWebPage
as
    select wp.PageId
          ,wp.AclId
          ,rp.ResPath
    from CK.tWebPage wp
    inner join CK.tResPath rp on wp.PageId = rp.ResId;
