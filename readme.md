Schema Zen - Script and create SQL Server objects quickly
--------------------------------------------------------

Schema Zen has two main commands:

**script**

    schemazen.exe script --server localhost --database db --scriptDir c:\somedir

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
**create**

    schemazen.exe create --server localhost --database db --scriptDir c:\somedir

This will create a database named db from the sql scripts in c:\somedir.

