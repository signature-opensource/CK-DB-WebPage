--[beginscript]

alter table CK.tWorkspace
    add PageId int not null default ( 0 ),
    constraint FK_CK_tWorkspace_PageId foreign key ( PageId ) references CK.tWebPage;

--[endscript]
