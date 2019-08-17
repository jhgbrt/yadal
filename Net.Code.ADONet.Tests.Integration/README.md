In order to run the integration tests for the different database drivers, 
a database should be running of the corresponding type.

These are the drivers tested:

* System.Data.SqlClient (Sql Server)
* System.Data.SqLite
* Oracle.ManagedDataAccess.Client
* MySql.Data.MySqlClient
* Npgsql (postgreSQL)
* IBM.DB2.Data.Core

For SqLite, no additional configuration should be necessary.

For the other databases, a script is provided that starts these databases as docker images
(see the start_databases.cmd script). This obviously requires Docker for windows to be installed.
For Oracle, this does not yet work as Oracle sadly does not provide a docker image for it's XE
database and I didn't get around to integrating a script to build such an image myself. 
(Pull request is welcome ;-))

