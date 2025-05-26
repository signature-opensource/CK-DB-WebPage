--[beginscript]

create table CK.tWebPageComponentType
(
	ComponentTypeId int identity( 0, 1 ) not null
        constraint PK_tWebPageComponentType primary key,

    TypeName varchar( 128 ) not null
        constraint UK_tWebPageComponentType_TypeName unique
);

insert into CK.tWebPageComponentType( TypeName ) values( '' );

--[endscript]
