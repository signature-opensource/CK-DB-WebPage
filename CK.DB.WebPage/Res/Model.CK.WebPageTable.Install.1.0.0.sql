--[beginscript]

create table CK.tWebPage
(
	PageId int not null,
    AclId int not null,
    ComponentTypeId int not null,
    constraint PK_CK_tWebPage primary key ( PageId ),
	constraint FK_CK_tWebPage_PageId foreign key ( PageId ) references CK.tResPath( ResId ),
	constraint FK_CK_tWebPage_AclId foreign key ( AclId ) references CK.tAcl( AclId ),
    constraint FK_CK_tWebPage_ComponentTypeId foreign key ( ComponentTypeId ) references CK.tWebPageComponentType( ComponentTypeId )
);

insert into CK.tWebPage( PageId, AclId, ComponentTypeId ) values( 0, 0, 0 );

--[endscript]
