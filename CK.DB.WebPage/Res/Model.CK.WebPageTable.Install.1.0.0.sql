--[beginscript]

create table CK.tWebPage
(
	PageId int not null,
    AclId int not null,
    constraint PK_CK_tWebPage primary key ( PageId ),
	constraint FK_CK_tWebPage_PageId foreign key ( PageId ) references CK.tResPath( ResId ),
	constraint FK_CK_tWebPage_AclId foreign key ( AclId ) references CK.tAcl( AclId ),
);

insert into CK.tWebPage( PageId, AclId ) values( 0, 0 );

--[endscript]
