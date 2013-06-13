Schemazen is a command line utility that makes it easy to put your database
in version control.

To create scripts for a databse schema run:

    schemazen.exe cn:"server=localhost;database=db;trusted_connection=yes;" db

Then to create the database from script run:

    schemazen.exe db cn:"server=localhost;database=db;trusted_connection=yes;"

