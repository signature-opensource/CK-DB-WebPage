-- SetupConfig: {}
create view CK.vWebPage
as
    select wp.PageId
          ,wp.AclId
          ,right(rp.ResPath, charindex('/', reverse(rp.ResPath)) - 1) as PageName
          ,IsNull(rppp.ParentResId, 0) as ParentPageId
    from CK.tWebPage wp
    inner join CK.tResPath rp
        on wp.PageId = rp.ResId
    left outer join CK.vResPathParentPrefixes rppp
        on wp.PageId = rppp.ResId and rppp.ParentLevel = 1;
