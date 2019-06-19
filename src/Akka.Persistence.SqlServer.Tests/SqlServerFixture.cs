using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Util;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace Akka.Persistence.SqlServer.Tests
{
    [CollectionDefinition("SqlServerSpec")]
    public sealed class SqlServerSpecsFixture : ICollectionFixture<SqlServerFixture>
    {

    }

    /// <summary>
    /// Fixture used to run SQL Server
    /// </summary>
    public class SqlServerFixture : IAsyncLifetime
    {
        protected string SqlServerImageName
        {
            get
            {
                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                //{
                //    return "microsoft/mssql-server-windows-express";
                //}
                //else // Linux or OS X
                //{
                    return "mcr.microsoft.com/mssql/server";
                //}
            }
        }

        protected string SqlServerImageTag
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "2017-latest";
                }
                else // Linux or OS X
                {
                    return "2017-latest-ubuntu";
                }
            }
        }

        protected readonly string SqlContainerName = $"sqlserver-{Guid.NewGuid():N}";
        protected DockerClient Client;

        public string ConnectionString { get; private set; }

        public SqlServerFixture()
        {
            DockerClientConfiguration config;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                config = new DockerClientConfiguration(new Uri("unix://var/run/docker.sock"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                config = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"));
            else
                throw new NotSupportedException($"Unsupported OS [{RuntimeInformation.OSDescription}]");

            Client = config.CreateClient();
        }

        public async Task InitializeAsync()
        {
            var images = await Client.Images.ListImagesAsync(new ImagesListParameters { MatchName = SqlServerImageName });
            if (images.Count == 0)
                await Client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = SqlServerImageName, Tag = "latest"}, null,
                    new Progress<JSONMessage>(message =>
                    {
                        Console.WriteLine(!string.IsNullOrEmpty(message.ErrorMessage)
                            ? message.ErrorMessage
                            : $"{message.ID} {message.Status} {message.ProgressMessage}");
                    }));

            var sqlServerHostPort = ThreadLocalRandom.Current.Next(9000, 10000);

            // create the container
            await Client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = SqlServerImageName,
                Name = SqlContainerName,
                Tty = true,
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "1433/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostPort = $"{sqlServerHostPort}"
                                }
                            }
                        }
                    },
                },
                Env = new string[] { "ACCEPT_EULA=Y", "sa_password=MyP@assword!1" }
            });

            // start the container
            await Client.Containers.StartContainerAsync(SqlContainerName, new ContainerStartParameters());

            // Provide a 30 second startup delay
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Execute the commands needed to setup the container
            var execInput = new ContainerExecCreateParameters()
            {
                AttachStderr = false,
                AttachStdin = false,
                AttachStdout = false,
                Cmd = new[] { "/opt/mssql-tools/bin/sqlcmd", "-S","localhost","-U","sa","-P", "MyP@assword!1", "-q", "\"CREATE LOGIN akkadotnet with password='akkadotnet', CHECK_POLICY=OFF; ALTER SERVER ROLE dbcreator ADD MEMBER akkadotnet;\"" },
                Detach = true,
                Tty = false,
                Privileged = false
            };
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var execRegistration = await Client.Containers.ExecCreateContainerAsync(SqlContainerName, execInput, cts.Token);
            await Client.Containers.StartContainerExecAsync(execRegistration.ID, cts.Token);
            var rsp = await Client.Containers.InspectContainerExecAsync(execRegistration.ID, cts.Token);

            while (rsp.Running)
            {
                // wait to finish
                rsp = await Client.Containers.InspectContainerExecAsync(execRegistration.ID, cts.Token);
            }
            
            if(rsp.ExitCode != 0)
                throw new InvalidOperationException("Unable to create initial schema in SQL Server. Aborting test run.");

            var connectionString = new DbConnectionStringBuilder
            {
                ConnectionString =
                    "data source=.;database=akka_persistence_tests;user id=akkadotnet;password=akkadotnet"
            };
            connectionString["Data Source"] = $"localhost:{sqlServerHostPort}";

            ConnectionString = connectionString.ToString();
        }

        public async Task DisposeAsync()
        {
            if (Client != null)
            {
                await Client.Containers.StopContainerAsync(SqlContainerName, new ContainerStopParameters());
                await Client.Containers.RemoveContainerAsync(SqlContainerName,
                    new ContainerRemoveParameters { Force = true });
                Client.Dispose();
            }
        }
    }
}
