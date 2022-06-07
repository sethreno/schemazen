using System.Collections.Generic;

namespace SchemaZen.Library.Models;

public class Permission : IScriptable, INameable {
	public Permission(
		string userName,
		string objectOwner,
		string objectName,
		string permissionType
	) {
		var nameParts = new List<string> {
			userName,
			objectOwner,
			objectName,
			permissionType
		};
		// don't include owner in the name if it's the default schema
		if (objectOwner == "dbo") nameParts.RemoveAt(1);
		Name = string.Join("___", nameParts);
		UserName = userName;
		ObjectOwner = objectOwner;
		ObjectName = objectName;
		PermissionType = permissionType;
	}

	// todo add Command property // GRANT, DENY, REVOKE
	public string UserName { get; set; }
	public string ObjectOwner { get; set; }
	public string ObjectName { get; set; }
	public string PermissionType { get; set; }
	public string Name { get; set; }

	public string ScriptCreate() {
		var objectFullName = ObjectOwner == "dbo"
			? $"[{ObjectName}]"
			: $"[{ObjectOwner}].[{ObjectName}]";

		return $@"GRANT {PermissionType} ON {objectFullName} TO [{UserName}]";
	}

	public string ScriptDrop() {
		return $@"REVOKE {PermissionType} ON [{ObjectOwner}] [{ObjectName}] TO [{UserName}]";
	}
}
