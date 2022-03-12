# Schema Zen - Script and create SQL Server objects quickly

## Schema Zen has three main commands:

### script

    dotnet schemazen script --server localhost --database db --scriptDir c:\somedir

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

    dotnet schemazen create --server localhost --database db --scriptDir c:\somedir

This will create a database named db from the sql scripts in c:\somedir.


### compare

	dotnet schemazen compare --source "server=dev;database=db" --target "server=qa;database=db" --outFile diff.sql

This will compare the databases named `db` between `dev` and `qa` and
create a sql script called `diff.sql` that can be run on `qa` to make it's
schema identical to `dev`.


See ```dotnet schemazen help [command]``` for more information and options on each command.

<br><br>

## Quick Start

If you don't already have a tool manifest in your project

    dotnet new tool-manifest

Install SchemaZen

    dotnet tool install SchemaZen

Script your database to disk

    dotnet schemazen script --server localhost --database db --scriptDir c:\somedir


## 1.x versions
SchemaZen was changed to a cross platform dotnet tool in version 2.0. Older 1.x
releases can be downloaded [here](https://github.com/sethreno/schemazen/releases)

[![Scc Count Badge](https://sloc.xyz/github/sethreno/schemazen/)](https://github.com/sethreno/schemazen/)
[![Scc Count Badge](https://sloc.xyz/github/sethreno/schemazen/?category=cocomo)](https://github.com/sethreno/schemazen/)

## Contributing
Pull requests are welcome and appreciated. See [contributing.md](contributing.md) for guidelines.

## Chat
[![Join the chat at https://gitter.im/sethreno/schemazen](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/sethreno/schemazen?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

----
If you've found Schema Zen helpful you can
[buy me a coffee](https://www.buymeacoffee.com/sethreno) to say thanks.
Cheers!
