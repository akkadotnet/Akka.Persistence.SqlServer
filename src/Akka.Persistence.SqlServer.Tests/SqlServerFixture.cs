// -----------------------------------------------------------------------
// <copyright file="SqlServerFixture.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Akka.Util;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;
using Xunit.Sdk;

namespace Akka.Persistence.SqlServer.Tests
{
    [CollectionDefinition("SqlServerSpec")]
    public sealed class SqlServerSpecsFixture : ICollectionFixture<SqlServerFixture>
    {
    }

    /// <summary>
    ///     Fixture used to run SQL Server
    /// </summary>
    public class SqlServerFixture : IAsyncLifetime
    {
        protected readonly string SqlContainerName = $"sqlserver-{Guid.NewGuid():N}";
        protected DockerClient Client;

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

        protected string ImageName => "mcr.microsoft.com/mssql/server";
        protected string Tag => "2017-latest";

        protected string SqlServerImageName => $"{ImageName}:{Tag}";

        public string ConnectionString { get; private set; }

        public async Task InitializeAsync()
        {
            var sysInfo = await Client.System.GetSystemInfoAsync();
            if (sysInfo.OSType.ToLowerInvariant() != "linux")
                throw new TestClassException("MSSQL docker image only available for linux containers");
            
            var images = await Client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "reference",
                        new Dictionary<string, bool>
                        {
                            {SqlServerImageName, true}
                        }
                    }
                }
            }); 

            if (images.Count == 0)
                await Client.Images.CreateImageAsync(
                    new ImagesCreateParameters {FromImage = ImageName, Tag = Tag}, null,
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
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    {"1433/tcp", new EmptyStruct()}
                },
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
                    }
                },
                Env = new[] {"ACCEPT_EULA=Y", "SA_PASSWORD=l0lTh1sIsOpenSource"}
            });

            // start the container
            await Client.Containers.StartContainerAsync(SqlContainerName, new ContainerStartParameters());

            // Wait until MSSQL is completely ready
            var logStream = await Client.Containers.GetContainerLogsAsync(SqlContainerName, new ContainerLogsParameters
            {
                Follow = true,
                ShowStdout = true,
                ShowStderr = true
            });

            string line = null;
            var timeoutInMilis = 60000;
            using (var reader = new StreamReader(logStream))
            {
                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < timeoutInMilis && (line = await reader.ReadLineAsync()) != null)
                {
                    if (line.Contains("SQL Server is now ready for client connections."))
                    {
                        break;
                    }
                }
                stopwatch.Stop();
            }
#if NET461
            logStream.Dispose();
#else
            await logStream.DisposeAsync();
#endif
            if (!line?.Contains("SQL Server is now ready for client connections.") ?? false)
                throw new Exception("MSSQL docker image failed to run.");
            
            var connectionString = new DbConnectionStringBuilder
            {
                ConnectionString =
                    "data source=.;database=akka_persistence_tests;user id=sa;password=l0lTh1sIsOpenSource"
            };
            connectionString["Data Source"] = $"localhost,{sqlServerHostPort}";

            ConnectionString = connectionString.ToString();
        }

        public async Task DisposeAsync()
        {
            if (Client != null)
            {
                try
                {
                    await Client.Containers.StopContainerAsync(SqlContainerName, new ContainerStopParameters());
                    await Client.Containers.RemoveContainerAsync(SqlContainerName,
                        new ContainerRemoveParameters {Force = true});
                }
                catch (DockerContainerNotFoundException)
                {
                    // no-op
                }
                finally
                {
                    Client.Dispose();
                }
            }
        }
    }
}