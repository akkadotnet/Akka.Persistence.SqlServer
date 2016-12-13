IF ($(docker ps -aq).Length -gt 0) {
    docker stop $(docker ps -aq)
    docker rm $(docker ps -aq)
}
Write-Host "Starting docker container..."
$container_id = docker run -d -p 1433:1433 -e sa_password=G!a7eZZM -e ACCEPT_EULA=Y microsoft/mssql-server-windows-express 
Start-Sleep -Seconds 30
docker exec $container_id sqlcmd -q "CREATE LOGIN akkadotnet with password='akkadotnet', CHECK_POLICY=OFF; ALTER SERVER ROLE dbcreator ADD MEMBER akkadotnet;"
$env:container_ip = docker inspect --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $container_id
$env:container_ip
