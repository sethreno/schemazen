Schema Zen - Script and create SQL Server objects quickly
--------------------------------------------------------

Schema Zen has two commands:

**script**

    schemazen.exe script --server localhost --database db --scriptDir c:\somedir

This will generate sql scripts for all objects in the database in the
following directory structure:

    c:\somedir\
      foreign_keys
	  functions
	  procs
	  tables
	  triggers
	  props.sql

**create**

    schemazen.exe create --server localhost --database db --scriptDir c:\somedir

This will create a database named db from the sql scripts in c:\somedir.


**dump**

	schemazen.exe dump -s "Data Source=.\SQLEXPRESS;Initial Catalog=MyCatalog;Integrated Security=True" -t "dump.xml"

This will create a dump of a database which can be used to compare to other databases.

**compare**

	schemazen.exe compare -s "Data Source=.\SQLEXPRESS;Initial Catalog=Database1;Integrated Security=True" -t "Data Source=.\SQLEXPRESS;Initial Catalog=Database2;Integrated Security=True"
	schemazen.exe compare -s "Data Source=.\SQLEXPRESS;Initial Catalog=Database1;Integrated Security=True" -c "db2.xml"
	schemazen.exe compare -x "db1.xml" -c "db2.xml"	

This will compare two databases or a dump with a database or two dumps. Differences will be shown as sql script. When using the parameter `-d="true"` a diff.xml will be created.

[![Build status](https://ci.appveyor.com/api/projects/status/3nobw7h1gq2gvpco)](https://ci.appveyor.com/project/brase/schemazen)

The latest release can be downloaded [here](https://github.com/sethreno/schemazen/releases)