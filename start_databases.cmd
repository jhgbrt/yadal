docker run --rm -d -p 3306:3306 --name mysql -e MYSQL_ROOT_PASSWORD=root mysql
docker run --rm -d -p 5432:5432 --name postgres -e POSTGRES_PASSWORD=P@ssword1! postgres
docker run --rm -d -p 1433:1433 --name sqlserver -e ACCEPT_EULA=Y -e SA_PASSWORD=P@ssword1! -e MSSQL_PID=Express mcr.microsoft.com/mssql/server:2017-latest-ubuntu
REM docker run --rm -d -p 50000:50000 -itd --name db2 --privileged=true -e LICENSE=accept -e DB2INST1_PASSWORD=P@ssword1! -e DBNAME=testdb ibmcom/db2
REM docker run --rm -d -p 1521:1521 -p 5500:5500 --name oracle -e ORACLE_PWD=P@ssword1! oracle/database:19.3.0-ee
