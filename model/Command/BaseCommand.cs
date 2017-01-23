using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SchemaZen.Library.Models;

namespace SchemaZen.Library.Command {
    public abstract class BaseCommand {
        public string Server { get; set; }
        public string DbName { get; set; }
        public string ConnectionString { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string ScriptDir { get; set; }
        public ILogger Logger { get; set; }
        public bool Overwrite { get; set; }
		public bool Merge { get; set; }
		public bool IgnoreDuplicateKeys { get; set; }

		public Database CreateDatabase(IList<string> filteredTypes = null, IList<string> filteredProps = null, bool collateColumns = false, string fileGroup = null)
        {
            filteredTypes = filteredTypes ?? new List<string>();

			filteredProps = filteredProps ?? new List<string>();

			if (!string.IsNullOrEmpty(ConnectionString))
            {
                if (!string.IsNullOrEmpty(Server) ||
                    !string.IsNullOrEmpty(DbName) ||
                    !string.IsNullOrEmpty(User) ||
                    !string.IsNullOrEmpty(Pass))
                {
                    throw new ArgumentException("You must not provide both a connection string and a server/db/user/password");
                }
                return new Database(filteredTypes, filteredProps)
                {
                    Connection = ConnectionString,
                    Dir = ScriptDir,
					CollateColumns = collateColumns,
					FileGroup = fileGroup
				};
            }
            if (string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(DbName))
            {
                throw new ArgumentException("You must provide a connection string, or a server and database name");
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = DbName,
                IntegratedSecurity = string.IsNullOrEmpty(User),
                //setting up pooling false to avoid re-use of connection while using the library.
                //http://www.c-sharpcorner.com/article/understanding-connection-pooling/
                Pooling = false
            };
            if (!builder.IntegratedSecurity)
            {
                builder.UserID = User;
                builder.Password = Pass;
            }
            return new Database(filteredTypes, filteredProps)
			{
                Connection = builder.ToString(),
                Dir = ScriptDir,
				CollateColumns = collateColumns,
				FileGroup = fileGroup
			};
        }

        public void AddDataTable(Database db, string name, string schema)
        {
            var t = db.FindTable(name, schema);
            if (t == null)
            {
                Console.WriteLine(
                    "warning: could not find data table {0}.{1}",
                    schema, name);
            }
            if (db.DataTables.Contains(t)) return;
            db.DataTables.Add(t);
        }
    }
}