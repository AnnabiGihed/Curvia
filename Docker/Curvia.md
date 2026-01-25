Create Docker : docker compose -p idea-tracker up -d
Login to KeyClock : 
	User : Administrator
	Password : 8uT*J1xY*QpL*w39vR5*
Connection to SQL Server from SSMS : 
	Server name: localhost,1433
	Authentication : SQL Server Authentication
	User : sa
	PWD : 8uT*J1xY*QpL*w39vR5*
	
Export Docker Configuration : docker exec keycloak /opt/keycloak/bin/kc.sh export ^
  --dir /opt/keycloak/data/export ^
  --users same_file

docker compose -p idea-tracker down

docker volume rm idea-tracker_mssql_data
docker volume rm idea-tracker_keycloak_persistent_data
