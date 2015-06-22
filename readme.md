Schema Zen - Script and create SQL Server objects quickly
--------------------------------------------------------

Schema Zen has three main commands:

## script

    SchemaZen.exe script --server localhost --database db --scriptDir c:\somedir

This will generate sql scripts for all objects in the database in the
following directory structure:
```
c:\somedir\

	foreign_keys
	functions
	procedures
	tables
	triggers
	views
	xmlschemacollections
	data
	props.sql
```
## create

    SchemaZen.exe create --server localhost --database db --scriptDir c:\somedir

This will create a database named db from the sql scripts in c:\somedir.

## compare

	SchemaZen.exe compare --source "Data Source=localhost;Initial Catalog=Database1;Integrated Security=True" --target "Data Source=localhost;Initial Catalog=Database2;Integrated Security=True" --outFile c:\somedir\diff.sql

This will compare the databases named Database1 and Database2 on localhost and create a sql script called c:\somedir\diff.sql that can be run on Database2 to make it identical to Database1.

---
## download SchemaZen
The latest release can be downloaded [here](https://github.com/sethreno/schemazen/releases)
