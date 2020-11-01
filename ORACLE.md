Oracle does not have a ready-to-use image in docker hub, but provides instructions to build your own.

On windows (with WSL):

* clone the oracle docker repository
* download the relevant linux ZIP image from https://www.oracle.com/database/technologies/oracle19c-linux-downloads.html
* move the downloaded zip file to docker-images/OracleDatabase/SingleInstance/[version]. At the time of writing, the version was 19.3.0.
* run the script below in wsl:


```
git clone https://github.com/oracle/docker-images
cd docker-images/OracleDatabase/SingleInstance
./buildDockerImage.sh -v 19.3.0 -e
```

Now, in docker for windows, verify that an image called `oracle/database` is present (using `docker images`):

```
PS C:\Users\jeroe\source\repos\jhgbrt\yadal> docker images
REPOSITORY                       TAG                  IMAGE ID       CREATED          SIZE
oracle/database                  19.3.0-ee            b6908adbecf6   17 minutes ago   6.54GB
```

To run the oracle image: 

```
docker run --name oracle -e ORACLE_PWD=P@ssword1! -p 1521:1521 -p 5500:5500 oracle/database:19.3.0-ee
```