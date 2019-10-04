param(
	[Parameter(Mandatory=$true)]
	[string]$dockerImage
)

IF ($(docker ps -aq -f label=deployer=akkadotnet).Length -gt 0) {
    docker stop $(docker ps -aq -f label=deployer=akkadotnet)
    docker rm $(docker ps -aq -f label=deployer=akkadotnet)
}

$env:container_name = $null
$env:container_ip = $null
Write-Host "Starting SQL Server Express Docker container..."
$env:container_name = "akka-$(New-Guid)"
$container_id = docker run -d --name $env:container_name -l deployer=akkadotnet -p 1433:1433 -e ACCEPT_EULA=Y $dockerImage
Start-Sleep -Seconds 30
docker exec $container_id sqlcmd -q "CREATE LOGIN akkadotnet with password='akkadotnet', CHECK_POLICY=OFF; ALTER SERVER ROLE dbcreator ADD MEMBER akkadotnet;"
$env:container_ip = docker inspect --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $container_id
[Environment]::SetEnvironmentVariable("container_name", $env:container_name, "User")
[Environment]::SetEnvironmentVariable("container_ip", $env:container_ip, "User")
Write-Host "SQL container started at IP addr: $($env:container_ip)"