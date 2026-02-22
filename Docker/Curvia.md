Create Docker : docker compose -p curvia up -d  / docker exec -it valhalla sh -lc 'ulimit -n'
Login to KeyClock : 
	User : Administrator
	Password : MyStrong@Pass123
Connection to SQL Server from SSMS : 
	Server name: localhost,1433
	Authentication : SQL Server Authentication
	User : sa
	PWD : 8uT*J1xY*QpL*w39vR5*
	
Export Docker Configuration : docker exec keycloak /opt/keycloak/bin/kc.sh export ^
  --dir /opt/keycloak/data/export ^
  --users same_file

docker compose -p curvia down

docker volume rm curvia_mssql_data
docker volume rm curvia_keycloak_persistent_data