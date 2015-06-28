Schema Zen - Script and create SQL Server objects quickly
--------------------------------------------------------

[![Join the chat at https://gitter.im/sethreno/schemazen](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/sethreno/schemazen?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Schema Zen has three main commands:

## script

    SchemaZen.exe script --server localhost --database db --scriptDir c:\somedir

This will generate sql scripts for all objects in the database in the
following directory structure:
```
c:\somedir\

	assemblies
	data
	foreign_keys
	functions
	procedures
	synonyms
	tables
	triggers
	users
	views
	xmlschemacollections
	props.sql
	schemas.sql
```
See ```SchemaZen.exe help script``` for more information, including how to specify which tables to export data from (none by default).
## create

    SchemaZen.exe create --server localhost --database db --scriptDir c:\somedir

This will create a database named db from the sql scripts in c:\somedir.
> Note that you can put additional scripts in a folder called ```after_data```, and it will run these between importing the data and adding the foreign key constraints, allowing you to "fix" any necessary records first.  (You will need to create this directory first, and the **script** command will *not* affect it.  The scripts will be run in alphabetical order, so you may want to prefix them with numbers if you want to enforce a certain order.  i.e. ```00001 - first script.sql```, ```00002 - second script.sql```)

## compare (experimental)

	SchemaZen.exe compare --source "Data Source=localhost;Initial Catalog=Database1;Integrated Security=True" --target "Data Source=localhost;Initial Catalog=Database2;Integrated Security=True" --outFile c:\somedir\diff.sql

This will compare the databases named Database1 and Database2 on localhost and create a sql script called c:\somedir\diff.sql that can be run on Database2 to make it's schema identical to Database1. **Warning** this feature is experimental. Be sure to test the script it generates before running in a production environment.

---
## download
The latest release can be downloaded [here](https://github.com/sethreno/schemazen/releases)

