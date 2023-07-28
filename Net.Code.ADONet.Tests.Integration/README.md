In order to run the integration tests for the different database drivers, 
a database should be running of the corresponding type.

These are the drivers tested:

* System.Data.SqlClient (Sql Server)
* System.Data.SqLite
* Oracle.ManagedDataAccess.Client
* MySql.Data.MySqlClient
* Npgsql (postgreSQL)
* <del>IBM.DB2.Data.Core</del> (should still be ok, but can't get DB2 container to work ATM)

For SqLite, no additional configuration should be necessary.

For the other databases, a script is provided that starts these databases as podman images
(see the start_databases.cmd script). This obviously requires Podman to be installed.

