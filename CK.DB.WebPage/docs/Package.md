Implements the `CK.tWebPage` table: a hierarchy of web pages named by their resource path (CK.DB.Res.ResPath), each
with its title, its ACL and the type of the component that displays it.

`WebPageTable` creates, renames and destroys the pages (`WebPageTable.CreateWebPageAsync`,
`WebPageTable.RenameWebPageAsync`, `WebPageTable.DestroyWebPageAsync`); `WebPageComponentTypeTable` manages the
component types (`CK.tWebPageComponentType`). The `CK.vWebPage` view lists the pages.
