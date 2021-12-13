namespace SchemaZen.Library.Models;

public class Permission : IScriptable, INameable {
	public Permission(string userName, string objectName, string permissionType) {
		Name = $"{userName}___{objectName}___{permissionType}";
		UserName = userName;
		ObjectName = objectName;
		PermissionType = permissionType;
	}

	public string UserName { get; set; }
	public string ObjectName { get; set; }
	public string PermissionType { get; set; }
	public string Name { get; set; }

	public string ScriptCreate() {
		return $@"GRANT {PermissionType} ON [{ObjectName}] TO [{UserName}]";
	}

	public string ScriptDrop() {
		return $@"REVOKE {PermissionType} ON [{ObjectName}] TO [{UserName}]";
	}
}
