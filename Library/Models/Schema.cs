namespace SchemaZen.Library.Models;

public class Schema : INameable, IHasOwner, IScriptable {
	public Schema(string name, string owner) {
		Owner = owner;
		Name = name;
	}

	public string Owner { get; set; }
	public string Name { get; set; }

	public string ScriptCreate() {
		// todo - determine if the check for existing schema and user is necessary
		// ideally this would simply return:
		//
		//    create schema [{Name}] authorization [{Owner}]
		return $@"
if not exists(select s.schema_id from sys.schemas s where s.name = '{Name}') 
	and exists(select p.principal_id from sys.database_principals p where p.name = '{Owner}') begin
	exec sp_executesql N'create schema [{Name}] authorization [{Owner}]'
end
";
	}
}
