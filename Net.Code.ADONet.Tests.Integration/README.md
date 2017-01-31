In order to run the integration tests for the different database drivers, 
a database should be running of the corresponding type.

These are the drivers tested:

* System.Data.SqlClient (Sql Server)
* System.Data.SqlServerCe.4.0 (Sql Server Compact)
* System.Data.SqLite
* Oracle.ManagedDataAccess.Client
* MySql.Data.MySqlClient
* Npgsql (postgreSQL)

For Sql Server Compact and SqLite and (in most cases) Sql Server, 
no additional configuration should be necessary (for Sql Server, the 
tests are configured to use (localdb)\MSSQLLocalDB with integrated 
security; if you use Visual Studio 2015 or higher this should be 
available out of the box).

For Oracle, MySQL and PostGreSQL, the database engines should be installed
and configured.

* Oracle Express Edition (XE)
* MySql
  downloads: https://dev.mysql.com/downloads/mysql/
  download the zip archive, not the installer
  extract 

* PostGreSQL
