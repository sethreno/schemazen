# Schema Zen - Script and create SQL Server objects quickly

[![Join the chat at https://gitter.im/sethreno/schemazen](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/sethreno/schemazen?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Schema Zen has three main commands:

### script

    SchemaZen.exe script --server localhost --database db --scriptDir c:\somedir

This will generate sql scripts for all objects in the database in a
directory structure that looks something like:
```
c:\somedir\
	data
	foreign_keys
	procedures
	tables
	views
	props.sql
	schemas.sql
```

### create

    SchemaZen.exe create --server localhost --database db --scriptDir c:\somedir

This will create a database named db from the sql scripts in c:\somedir.


### compare

	SchemaZen.exe compare --source "server=dev;database=db" --target "server=qa;database=db" --outFile diff.sql

This will compare the databases named `db` between `dev` and `qa` and
create a sql script called `diff.sql` that can be run on `qa` to make it's
schema identical to `dev`.


See ```SchemaZen.exe help [command]``` for more information and options on each command.

## download
The latest release can be downloaded [here](https://github.com/sethreno/schemazen/releases)

## contributing
Pull requests are welcome and appreciated. See [contributing.md](contributing.md) for guidelines.

