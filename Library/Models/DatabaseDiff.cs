using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models;

public class DatabaseDiff {
	public List<SqlAssembly> AssembliesAdded = new();
	public List<SqlAssembly> AssembliesDeleted = new();
	public List<SqlAssembly> AssembliesDiff = new();
	public Database Db;
	public List<ForeignKey> ForeignKeysAdded = new();
	public List<ForeignKey> ForeignKeysDeleted = new();
	public List<ForeignKey> ForeignKeysDiff = new();
	public List<Permission> PermissionsAdded = new();
	public List<Permission> PermissionsDeleted = new();
	public List<Permission> PermissionsDiff = new();
	public List<DbProp> PropsChanged = new();

	public List<Routine> RoutinesAdded = new();
	public List<Routine> RoutinesDeleted = new();
	public List<Routine> RoutinesDiff = new();
	public List<Synonym> SynonymsAdded = new();
	public List<Synonym> SynonymsDeleted = new();
	public List<Synonym> SynonymsDiff = new();
	public List<Table> TablesAdded = new();
	public List<Table> TablesDeleted = new();
	public List<TableDiff> TablesDiff = new();
	public List<Table> TableTypesDiff = new();
	public List<SqlUser> UsersAdded = new();
	public List<SqlUser> UsersDeleted = new();
	public List<SqlUser> UsersDiff = new();
	public List<Constraint> ViewIndexesAdded = new();
	public List<Constraint> ViewIndexesDeleted = new();
	public List<Constraint> ViewIndexesDiff = new();

	public bool IsDiff => PropsChanged.Count > 0
		|| TablesAdded.Count > 0
		|| TablesDiff.Count > 0
		|| TableTypesDiff.Count > 0
		|| TablesDeleted.Count > 0
		|| RoutinesAdded.Count > 0
		|| RoutinesDiff.Count > 0
		|| RoutinesDeleted.Count > 0
		|| ForeignKeysAdded.Count > 0
		|| ForeignKeysDiff.Count > 0
		|| ForeignKeysDeleted.Count > 0
		|| AssembliesAdded.Count > 0
		|| AssembliesDiff.Count > 0
		|| AssembliesDeleted.Count > 0
		|| UsersAdded.Count > 0
		|| UsersDiff.Count > 0
		|| UsersDeleted.Count > 0
		|| ViewIndexesAdded.Count > 0
		|| ViewIndexesDiff.Count > 0
		|| ViewIndexesDeleted.Count > 0
		|| SynonymsAdded.Count > 0
		|| SynonymsDiff.Count > 0
		|| SynonymsDeleted.Count > 0
		|| PermissionsAdded.Count > 0
		|| PermissionsDiff.Count > 0
		|| PermissionsDeleted.Count > 0;

	private static string Summarize(bool includeNames, List<string> changes, string caption) {
		if (changes.Count == 0) return string.Empty;
		return changes.Count + "x " + caption +
			(includeNames ? "\r\n\t" + string.Join("\r\n\t", changes.ToArray()) : string.Empty) +
			"\r\n";
	}

	public string SummarizeChanges(bool includeNames) {
		var sb = new StringBuilder();
		sb.Append(
			Summarize(
				includeNames,
				AssembliesAdded.Select(o => o.Name).ToList(),
				"assemblies in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				AssembliesDeleted.Select(o => o.Name).ToList(),
				"assemblies not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				AssembliesDiff.Select(o => o.Name).ToList(),
				"assemblies altered"));
		sb.Append(
			Summarize(
				includeNames,
				ForeignKeysAdded.Select(o => o.Name).ToList(),
				"foreign keys in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				ForeignKeysDeleted.Select(o => o.Name).ToList(),
				"foreign keys not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				ForeignKeysDiff.Select(o => o.Name).ToList(),
				"foreign keys altered"));
		sb.Append(
			Summarize(
				includeNames,
				PropsChanged.Select(o => o.Name).ToList(),
				"properties changed"));
		sb.Append(
			Summarize(
				includeNames,
				RoutinesAdded.Select(o => $"{o.RoutineType.ToString()} {o.Owner}.{o.Name}")
					.ToList(),
				"routines in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				RoutinesDeleted.Select(o => $"{o.RoutineType.ToString()} {o.Owner}.{o.Name}")
					.ToList(),
				"routines not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				RoutinesDiff.Select(o => $"{o.RoutineType.ToString()} {o.Owner}.{o.Name}").ToList(),
				"routines altered"));
		sb.Append(
			Summarize(
				includeNames,
				TablesAdded.Where(o => !o.IsType).Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"tables in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				TablesDeleted.Where(o => !o.IsType).Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"tables not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				TablesDiff.Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"tables altered"));
		sb.Append(
			Summarize(
				includeNames,
				TablesAdded.Where(o => o.IsType).Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"table types in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				TablesDeleted.Where(o => o.IsType).Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"table types not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				TableTypesDiff.Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"table types altered"));
		sb.Append(
			Summarize(
				includeNames,
				UsersAdded.Select(o => o.Name).ToList(),
				"users in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				UsersDeleted.Select(o => o.Name).ToList(),
				"users not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				UsersDiff.Select(o => o.Name).ToList(),
				"users altered"));
		sb.Append(
			Summarize(
				includeNames,
				ViewIndexesAdded.Select(o => o.Name).ToList(),
				"view indexes in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				ViewIndexesDeleted.Select(o => o.Name).ToList(),
				"view indexes not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				ViewIndexesDiff.Select(o => o.Name).ToList(),
				"view indexes altered"));
		sb.Append(
			Summarize(
				includeNames,
				SynonymsAdded.Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"synonyms in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				SynonymsDeleted.Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"synonyms not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				SynonymsDiff.Select(o => $"{o.Owner}.{o.Name}").ToList(),
				"synonyms altered"));
		sb.Append(
			Summarize(
				includeNames,
				PermissionsAdded.Select(o => $"{o.ObjectName}: {o.PermissionType} TO {o.UserName}")
					.ToList(),
				"permissions in source but not in target"));
		sb.Append(
			Summarize(
				includeNames,
				PermissionsDeleted
					.Select(o => $"{o.ObjectName}: {o.PermissionType} TO {o.UserName}")
					.ToList(),
				"permissions not in source but in target"));
		sb.Append(
			Summarize(
				includeNames,
				PermissionsDiff.Select(o => $"{o.ObjectName}: {o.PermissionType} TO {o.UserName}")
					.ToList(),
				"permissions altered"));
		return sb.ToString();
	}

	public string Script() {
		var text = new StringBuilder();
		//alter database props
		//TODO need to check dependencies for collation change
		//TODO how can collation be set to null at the server level?
		if (PropsChanged.Count > 0) {
			text.Append(Database.ScriptPropList(PropsChanged));
			text.AppendLine("GO");
			text.AppendLine();
		}

		//delete foreign keys
		if (ForeignKeysDeleted.Count + ForeignKeysDiff.Count > 0) {
			foreach (var fk in ForeignKeysDeleted) text.AppendLine(fk.ScriptDrop());

			//delete modified foreign keys
			foreach (var fk in ForeignKeysDiff) text.AppendLine(fk.ScriptDrop());

			text.AppendLine("GO");
		}

		//delete tables
		if (TablesDeleted.Count + TableTypesDiff.Count > 0) {
			foreach (var t in TablesDeleted.Concat(TableTypesDiff)) text.AppendLine(t.ScriptDrop());

			text.AppendLine("GO");
		}
		// TODO: table types drop will fail if anything references them... try to find a workaround?

		//modify tables
		if (TablesDiff.Count > 0) {
			foreach (var t in TablesDiff) text.Append(t.Script());

			text.AppendLine("GO");
		}

		//add tables
		if (TablesAdded.Count + TableTypesDiff.Count > 0) {
			foreach (var t in TablesAdded.Concat(TableTypesDiff)) text.Append(t.ScriptCreate());

			text.AppendLine("GO");
		}

		//add foreign keys
		if (ForeignKeysAdded.Count + ForeignKeysDiff.Count > 0) {
			foreach (var fk in ForeignKeysAdded) text.AppendLine(fk.ScriptCreate());

			//add modified foreign keys
			foreach (var fk in ForeignKeysDiff) text.AppendLine(fk.ScriptCreate());

			text.AppendLine("GO");
		}

		//add & delete procs, functions, & triggers
		foreach (var r in RoutinesAdded) {
			text.AppendLine(r.ScriptCreate());
			text.AppendLine("GO");
		}

		foreach (var r in RoutinesDiff) // script alter if possible, otherwise drop and (re)create
			try {
				text.AppendLine(r.ScriptAlter(Db));
				text.AppendLine("GO");
			} catch {
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
				text.AppendLine(r.ScriptCreate());
				text.AppendLine("GO");
			}

		foreach (var r in RoutinesDeleted) {
			text.AppendLine(r.ScriptDrop());
			text.AppendLine("GO");
		}

		//add & delete synonyms
		foreach (var s in SynonymsAdded) {
			text.AppendLine(s.ScriptCreate());
			text.AppendLine("GO");
		}

		foreach (var s in SynonymsDiff) {
			text.AppendLine(s.ScriptDrop());
			text.AppendLine("GO");
			text.AppendLine(s.ScriptCreate());
			text.AppendLine("GO");
		}

		foreach (var s in SynonymsDeleted) {
			text.AppendLine(s.ScriptDrop());
			text.AppendLine("GO");
		}

		//add & delete permissions
		foreach (var p in PermissionsAdded) {
			text.AppendLine(p.ScriptCreate());
			text.AppendLine("GO");
		}

		foreach (var p in PermissionsDiff) {
			text.AppendLine(p.ScriptDrop());
			text.AppendLine("GO");
			text.AppendLine(p.ScriptCreate());
			text.AppendLine("GO");
		}

		foreach (var p in PermissionsDeleted) {
			text.AppendLine(p.ScriptDrop());
			text.AppendLine("GO");
		}

		return text.ToString();
	}
}
