docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ������������˿�������Ϊmssql��û��������
docker exec -i mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Fuckme123!@#" -Q "ALTER LOGIN SA WITH PASSWORD='Fuckme123!@#'"