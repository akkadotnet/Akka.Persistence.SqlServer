#IF ($(docker ps -aq -f label=deployer=akkadotnet).Length -gt 0) {
#    docker stop $(docker ps -aq -f label=deployer=akkadotnet)
#    docker rm $(docker ps -aq -f label=deployer=akkadotnet)
#}
#Write-Host "Starting SQL Server Express Docker container..."
#$env:container_name = "akka-$(New-Guid)"
#$container_id = docker run -d --name $env:container_name -l deployer=akkadotnet -p 1433:1433 -e ACCEPT_EULA=Y microsoft/mssql-server-windows-express
#Start-Sleep -Seconds 30
#docker exec $container_id sqlcmd -q "CREATE LOGIN akkadotnet with password='akkadotnet', CHECK_POLICY=OFF; ALTER SERVER ROLE dbcreator ADD MEMBER akkadotnet;"
#$env:container_ip = docker inspect --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $container_id
#Write-Host "SQL container started at IP addr: $($env:container_ip)"

IF ($(docker ps -aq -f label=deployer=akkadotnet).Length -gt 0) {
    docker stop $(docker ps -aq -f label=deployer=akkadotnet)
    docker rm $(docker ps -aq -f label=deployer=akkadotnet)
}
docker run -it --name $env:container_name -l deployer=akkadotnet -p 1433:1433 -e ACCEPT_EULA=Y microsoft/mssql-server-windows-express