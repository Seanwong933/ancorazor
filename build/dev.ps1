docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ������������˿�������Ϊmssql��û�����������Ȼ������һ�����ԡ�
# If it fails may because mssql hasn't launch completely, just try again later.
docker exec -i mssql /opt/mssql-tools/bin/sqlcmd `
	-S localhost `
	-U SA -P "Fuckme123!@#" `
	-Q "ALTER LOGIN SA WITH PASSWORD='Fuckme123!@#'"

docker run --rm -it `
	-v F:\Projects\siegrain.blog\:/app/ `
	-v F:\Projects\siegrain.blog\Blog.API\appsettings.Development.docker.json:/app/Blog.API/appsettings.Development.json `
	-p 8088:8088 `
	--link skywalking-oap `
	--link mssql `
	--name ancorazor_dev `
	--network build_default `
 	-w /app/Blog.API siegrainwong/aspnetcore-build:2.2-bionic dotnet watch run