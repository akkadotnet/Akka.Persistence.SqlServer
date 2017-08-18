#I @"tools/FAKE/tools"
#r "FakeLib.dll"
#r "System.Management.Automation"

open System
open System.IO
open System.Text

open Fake
open Fake.DotNetCli
open Fake.DocFxHelper
open System.Management.Automation
open System.Data.Common
open Fake.FileHelper

//--------------------------------------------------------------------------------
// Information about the project for Nuget and Assembly info files
//--------------------------------------------------------------------------------


let product = "Akka.NET"
let authors = [ "Akka.NET Team" ]
let copyright = "Copyright © 2013-2017 Akka.NET Team"
let company = "Akka.NET Team"
let description = "Akka.NET is a port of the popular Java/Scala framework Akka to .NET"
let tags = ["akka";"actors";"actor";"model";"Akka";"concurrency"]
let configuration = "Release"

// Read release notes and version

let parsedRelease =
    File.ReadLines "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let envBuildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER")
let buildNumber = if String.IsNullOrWhiteSpace(envBuildNumber) then "0" else envBuildNumber

let version = parsedRelease.AssemblyVersion + "." + buildNumber
let preReleaseVersion = version + "-beta"

let isUnstableDocs = hasBuildParam "unstable"
let isPreRelease = hasBuildParam "nugetprerelease"
let release = if isPreRelease then ReleaseNotesHelper.ReleaseNotes.New(version, version + "-beta", parsedRelease.Notes) else parsedRelease
let versionSuffix = 
    match (getBuildParam "nugetprerelease") with
    | "dev" -> (if (not (buildNumber = "0")) then (buildNumber) else "") + "-beta"
    | _ -> ""

printfn "Assembly version: %s\nNuget version; %s\n" release.AssemblyVersion release.NugetVersion
//--------------------------------------------------------------------------------
// Directories

let output = __SOURCE_DIRECTORY__  @@ "bin"
let outputTests = output @@ "TestResults"
let outputPerfTests = output @@ "perf"
let outputBinaries = output @@ "binaries"
let outputNuGet = output @@ "nuget"
let outputBinariesNet45 = outputBinaries @@ "net45"
let outputBinariesNetStandard = outputBinaries @@ "netstandard1.6"
let slnFile = "./src/Akka.Persistence.SqlServer.sln"

Target "RestorePackages" (fun _ ->
    let additionalArgs = if versionSuffix.Length > 0 then [sprintf "/p:VersionSuffix=%s" versionSuffix] else []  

    DotNetCli.Restore
        (fun p -> 
            { p with
                Project = slnFile
                NoCache = false 
                AdditionalArgs = additionalArgs })
)

//--------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDir output
    CleanDir outputTests
    CleanDir outputPerfTests
    CleanDir outputNuGet
    CleanDir "docs/_site"
    CleanDirs !! "./**/bin"
    CleanDirs !! "./**/obj"
)

//--------------------------------------------------------------------------------
// Build the solution

Target "Build" (fun _ ->
    let additionalArgs = if versionSuffix.Length > 0 then [sprintf "/p:VersionSuffix=%s" versionSuffix] else []  

    let projects = !! "./**/*.csproj"

    let runSingleProject project =
        DotNetCli.Build
            (fun p -> 
                { p with
                    Project = project
                    Configuration = configuration 
                    AdditionalArgs = additionalArgs })

    projects |> Seq.iter (runSingleProject)
)

Target "BuildRelease" DoNothing



//--------------------------------------------------------------------------------
// Tests targets
//--------------------------------------------------------------------------------
module internal ResultHandling =
    let (|OK|Failure|) = function
        | 0 -> OK
        | x -> Failure x

    let buildErrorMessage = function
        | OK -> None
        | Failure errorCode ->
            Some (sprintf "xUnit2 reported an error (Error Code %d)" errorCode)

    let failBuildWithMessage = function
        | DontFailBuild -> traceError
        | _ -> (fun m -> raise(FailedTestsException m))

    let failBuildIfXUnitReportedError errorLevel =
        buildErrorMessage
        >> Option.iter (failBuildWithMessage errorLevel)

//--------------------------------------------------------------------------------
// Clean test output

Target "CleanTests" <| fun _ ->
    CleanDir outputTests
//--------------------------------------------------------------------------------
// Run tests

open Fake.Testing
Target "RunTests" <| fun _ ->  
    let projects = 
        match (isWindows) with 
        | true -> !! "./src/**/*.Tests.csproj"
        | _ -> !! "./src/**/*.Tests.csproj" // if you need to filter specs for Linux vs. Windows, do it here

    ensureDirectory outputTests

    let runSingleProject project =
        DotNetCli.Test
                (fun p -> 
                    { p with
                        Project = project
                        Configuration = configuration })

    projects |> Seq.iter (log)
    projects |> Seq.iter (runSingleProject)

Target "StartDbContainer" <| fun _ ->
    let dockerImage = getBuildParamOrDefault "dockerImage" @"microsoft/mssql-server-windows-express"
    logfn "Starting SQL Express Docker container using image: %s" dockerImage
    let posh = PowerShell.Create().AddScript(sprintf @"./docker_sql_express.ps1 -dockerImage %s" dockerImage)
    posh.Invoke() |> Seq.iter (logfn "%O")

    match posh.HadErrors with
    | true -> posh.Streams.Error |> Seq.iter (logfn "\t %O")
              failwith "SQL Express Docker container startup encountered an error... failing build"
    | false -> ()

    match environVarOrNone "container_ip" with
    | Some x -> logfn "SQL Express Docker container created with IP address: %s" x
    | None -> failwith "SQL Express Docker container env:container_ip not set... failing build"

Target "PrepAppConfig" <| fun _ -> 
    let ip = environVarOrNone "container_ip"
    match ip with
    | Some ip ->
        let appConfig = !! "src/Akka.Persistence.SqlServer.Tests/bin/Release/**/app.xml"

        let updateConfig config =          
          let configFile = readConfig config
          let connStringNode = configFile.SelectSingleNode "//connectionStrings/add[@name='TestDb']"
          let connString = connStringNode.Attributes.["connectionString"].Value

          log ("Existing App.config connString: " + Environment.NewLine + "\t" + connString)

          let newConnString = new DbConnectionStringBuilder();
          newConnString.ConnectionString <- connString
          newConnString.Item("data source") <- ip
      
          log ("New App.config connString: " + Environment.NewLine + "\t" + newConnString.ToString())

          updateConnectionString "TestDb" (newConnString.ToString()) config

        appConfig |> Seq.iter(updateConfig)

    | None -> failwith "SQL Express Docker container not started successfully $env:container_ip not found... failing build"

FinalTarget "TearDownDbContainer" <| fun _ ->
    match environVarOrNone "container_name" with
    | Some x -> let cmd = sprintf "docker stop %s; docker rm %s" x x
                logf "Killing container: %s" x
                PowerShell.Create()
                    .AddScript(cmd)
                    .Invoke()
                    |> ignore
    | None -> ()

Target "ActivateFinalTargets"  <| fun _ ->
    ActivateFinalTarget "TearDownDbContainer"

//--------------------------------------------------------------------------------
// Nuget targets 
//--------------------------------------------------------------------------------
Target "Nuget" DoNothing

let overrideVersionSuffix (project:string) =
    match project with
    | _ -> versionSuffix // add additional matches to publish different versions for different projects in solution
Target "CreateNuget" (fun _ ->    
    let projects = !! "src/**/*.csproj" 
                   -- "src/**/*Tests.csproj" // Don't publish unit tests
                   -- "src/**/*Tests*.csproj"

    let runSingleProject project =
        DotNetCli.Pack
            (fun p -> 
                { p with
                    Project = project
                    Configuration = configuration
                    AdditionalArgs = ["--include-symbols"]
                    VersionSuffix = overrideVersionSuffix project
                    OutputPath = outputNuGet })

    projects |> Seq.iter (runSingleProject)
)

Target "PublishNuget" (fun _ ->
    let projects = !! "./bin/nuget/*.nupkg" -- "./bin/nuget/*.symbols.nupkg"
    let apiKey = getBuildParamOrDefault "nugetkey" ""
    let source = getBuildParamOrDefault "nugetpublishurl" ""
    let symbolSource = getBuildParamOrDefault "symbolspublishurl" ""
    let shouldPublishSymbolsPackages = not (symbolSource = "")

    if (not (source = "") && not (apiKey = "") && shouldPublishSymbolsPackages) then
        let runSingleProject project =
            DotNetCli.RunCommand
                (fun p -> 
                    { p with 
                        TimeOut = TimeSpan.FromMinutes 10. })
                (sprintf "nuget push %s --api-key %s --source %s --symbol-source %s" project apiKey source symbolSource)

        projects |> Seq.iter (runSingleProject)
    else if (not (source = "") && not (apiKey = "") && not shouldPublishSymbolsPackages) then
        let runSingleProject project =
            DotNetCli.RunCommand
                (fun p -> 
                    { p with 
                        TimeOut = TimeSpan.FromMinutes 10. })
                (sprintf "nuget push %s --api-key %s --source %s" project apiKey source)

        projects |> Seq.iter (runSingleProject)
)




//--------------------------------------------------------------------------------
// Help 
//--------------------------------------------------------------------------------

Target "Help" <| fun _ ->
    List.iter printfn [
      "usage:"
      "build [target]"
      ""
      " Targets for building:"
      " * Build      Builds"
      " * Nuget      Create and optionally publish nugets packages"
      " * RunTests   Runs tests"
      " * RunTestsWithDocker Runs tests against a Docker container using the microsoft/sql-server-windows-express image"
      " * All        Builds, run tests, creates and optionally publish nuget packages"
      " * AllWithDockerTests Builds, runs Docker container-based tests, creates and optionally publish nuget packages"
      ""
      " Other Targets"
      " * Help       Display this help" 
      " * HelpNuget  Display help about creating and pushing nuget packages" 
      " * HelpDocs   Display help about creating and pushing API docs" 
      ""]

Target "HelpNuget" <| fun _ ->
    List.iter printfn [
      "usage: "
      "build Nuget [nugetkey=<key> [nugetpublishurl=<url>]] "
      "            [symbolskey=<key> symbolspublishurl=<url>] "
      "            [nugetprerelease=<prefix>]"
      ""
      "Arguments for Nuget target:"
      "   nugetprerelease=<prefix>   Creates a pre-release package."
      "                              The version will be version-prefix<date>"
      "                              Example: nugetprerelease=dev =>"
      "                                       0.6.3-dev1408191917"
      ""
      "In order to publish a nuget package, keys must be specified."
      "If a key is not specified the nuget packages will only be created on disk"
      "After a build you can find them in bin/nuget"
      ""
      "For pushing nuget packages to nuget.org and symbols to symbolsource.org"
      "you need to specify nugetkey=<key>"
      "   build Nuget nugetKey=<key for nuget.org>"
      ""
      "For pushing the ordinary nuget packages to another place than nuget.org specify the url"
      "  nugetkey=<key>  nugetpublishurl=<url>  "
      ""
      "For pushing symbols packages specify:"
      "  symbolskey=<key>  symbolspublishurl=<url> "
      ""
      "Examples:"
      "  build Nuget                      Build nuget packages to the bin/nuget folder"
      ""
      "  build Nuget nugetprerelease=dev  Build pre-release nuget packages"
      ""
      "  build Nuget nugetkey=123         Build and publish to nuget.org and symbolsource.org"
      ""
      "  build Nuget nugetprerelease=dev nugetkey=123 nugetpublishurl=http://abc"
      "              symbolskey=456 symbolspublishurl=http://xyz"
      "                                   Build and publish pre-release nuget packages to http://abc"
      "                                   and symbols packages to http://xyz"
      ""]

Target "HelpDocs" <| fun _ ->
    List.iter printfn [
      "usage: "
      "build Docs"
      "Just builds the API docs for Akka.NET locally. Does not attempt to publish."
      ""
      "build PublishDocs azureKey=<key> "
      "                  azureUrl=<url> "
      "                 [unstable=true]"
      ""
      "Arguments for PublishDocs target:"
      "   azureKey=<key>             Azure blob storage key."
      "                              Used to authenticate to the storage account."
      ""
      "   azureUrl=<url>             Base URL for Azure storage container."
      "                              FAKE will automatically set container"
      "                              names based on build parameters."
      ""
      "   [unstable=true]            Indicates that we'll publish to an Azure"
      "                              container named 'unstable'. If this param"
      "                              is not present we'll publish to containers"
      "                              'stable' and the 'release.version'"
      ""
      "In order to publish documentation all of these values must be provided."
      "Examples:"
      "  build PublishDocs azureKey=1s9HSAHA+..."
      "                    azureUrl=http://fooaccount.blob.core.windows.net/docs"
      "                                   Build and publish docs to http://fooaccount.blob.core.windows.net/docs/stable"
      "                                   and http://fooaccount.blob.core.windows.net/docs/{release.version}"
      ""
      "  build PublishDocs azureKey=1s9HSAHA+..."
      "                    azureUrl=http://fooaccount.blob.core.windows.net/docs"
      "                    unstable=true"
      "                                   Build and publish docs to http://fooaccount.blob.core.windows.net/docs/unstable"
      ""]

//--------------------------------------------------------------------------------
//  Target dependencies
//--------------------------------------------------------------------------------

// build dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "BuildRelease"

// tests dependencies
"CleanTests" ==> "RunTests"

// tests with docker dependencies
Target "RunTestsWithDocker" DoNothing
"CleanTests" ==> "ActivateFinalTargets" ==> "StartDbContainer" ==> "PrepAppConfig" ==> "RunTests" ==> "RunTestsWithDocker"

// nuget dependencies
"BuildRelease" ==> "CreateNuget" ==> "Nuget"

Target "All" DoNothing
"BuildRelease" ==> "All"
"RunTests" ==> "All"
"Nuget" ==> "All"

Target "AllWithDockerTests" DoNothing
"BuildRelease" ==> "AllWithDockerTests"
"RunTestsWithDocker" ==> "AllWithDockerTests"
"Nuget" ==> "AllWithDockerTests"

RunTargetOrDefault "Help"

