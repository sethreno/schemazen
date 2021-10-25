namespace SchemaZen.Library.Models
{
	public class Permission : IScriptable, INameable
	{
		public string Name { get; set; }
		public string UserName { get; set; }
		public string ObjectName { get; set; }
		public string ObjectSchemaName { get; set; }
		public string PermissionType { get; set; }

		public Permission(string userName, string objectName, string permissionType, string objectSchemaName)
		{
			Name = $"{userName}___{objectSchemaName}___{objectName}___{permissionType}";
			UserName = userName;
			ObjectName = objectName;
			PermissionType = permissionType;
			ObjectSchemaName = objectSchemaName;
		}

		public string ScriptCreate()
		{
			return $@"GRANT {PermissionType} ON [{ObjectSchemaName}].[{ObjectName}] TO [{UserName}]";
		}

		public string ScriptDrop()
		{
			return $@"REVOKE {PermissionType} ON [{ObjectSchemaName}].[{ObjectName}] TO [{UserName}]";
		}
	}
}
