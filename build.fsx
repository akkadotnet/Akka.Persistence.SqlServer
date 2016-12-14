#I @"src/packages/FAKE/tools"
#r "FakeLib.dll"
#r "System.Xml.Linq"
#r "System.Management.Automation"

open System
open System.IO
open System.Text
open System.Management.Automation
open System.Data.Common
open Fake
open Fake.FileUtils
open Fake.TaskRunnerHelper
open Fake.ProcessHelper
open Fake.EnvironmentHelper
open Fake.ConfigurationHelper

cd __SOURCE_DIRECTORY__

//--------------------------------------------------------------------------------
// Information about the project for Nuget and Assembly info files
//--------------------------------------------------------------------------------


let product = "Akka.NET"
let authors = [ "Akka.NET Team" ]
let copyright = "Copyright © 2013-2015 Akka.NET Team"
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

printfn "Assembly version: %s\nNuget version; %s\n" release.AssemblyVersion release.NugetVersion
//--------------------------------------------------------------------------------
// Directories

let binDir = "bin"
let testOutput = "TestResults"

let nugetDir = binDir @@ "nuget"
let workingDir = binDir @@ "build"
let libDir = workingDir @@ @"lib\net45\"
let nugetExe = FullName @"src\.nuget\NuGet.exe"
let slnFile = "./src/Akka.Persistence.SqlServer.sln"

open Fake.RestorePackageHelper
Target "RestorePackages" (fun _ -> 
     slnFile
     |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = "./src/packages"
             Retries = 4 })
 )

//--------------------------------------------------------------------------------
// Clean build results

Target "Clean" <| fun _ ->
    DeleteDir binDir

//--------------------------------------------------------------------------------
// Generate AssemblyInfo files with the version for release notes 

open AssemblyInfoFile

Target "AssemblyInfo" <| fun _ ->
    CreateCSharpAssemblyInfoWithConfig "src/SharedAssemblyInfo.cs" [
        Attribute.Company company
        Attribute.Copyright copyright
        Attribute.Trademark ""
        Attribute.Version version
        Attribute.FileVersion version ] <| AssemblyInfoFileConfig(false)


//--------------------------------------------------------------------------------
// Build the solution

Target "Build" <| fun _ ->

    !! slnFile
    |> MSBuildRelease "" "Rebuild"
    |> ignore


//--------------------------------------------------------------------------------
// Copy the build output to bin directory
//--------------------------------------------------------------------------------

Target "CopyOutput" <| fun _ ->
    
    let copyOutput project =
        let src = "src" @@ project @@ @"bin/Release/"
        let dst = binDir @@ project
        CopyDir dst src allFiles
    [ "Akka.Persistence.SqlServer"
      ]
    |> List.iter copyOutput

Target "BuildRelease" DoNothing



//--------------------------------------------------------------------------------
// Tests targets
//--------------------------------------------------------------------------------

//--------------------------------------------------------------------------------
// Clean test output

Target "CleanTests" <| fun _ ->
    DeleteDir testOutput
//--------------------------------------------------------------------------------
// Run tests

open Fake.Testing
Target "RunTests" <| fun _ ->  
    let xunitTestAssemblies = !! "src/**/bin/Release/*.Tests.dll" 

    mkdir testOutput

    let xunitToolPath = findToolInSubPath "xunit.console.exe" "src/packages/xunit.runner.console*/tools"
    printfn "Using XUnit runner: %s" xunitToolPath
    xUnit2
        (fun p -> { p with HtmlOutputPath = Some(testOutput @@ "xunit.html") })
        xunitTestAssemblies

Target "StartDbContainer" <| fun _ -> 
    PowerShell.Create()
        .AddScript(@"./docker_sql_express.ps1")
        .Invoke()
        |> Seq.last
        |> printfn "SQL Express Docker container created with IP address: %O"

Target "PrepAppConfig" <| fun _ -> 
    let ip = environVar "container_ip"
    let appConfig = "src/Akka.Persistence.SqlServer.Tests/App.config"

    log appConfig
    log ip

    let configFile = readConfig appConfig
    let connStringNode = configFile.SelectSingleNode "//connectionStrings/add[@name='TestDb']"
    let connString = connStringNode.Attributes.["connectionString"].Value

    log ("Existing App.config connString: " + Environment.NewLine + "\t" + connString)

    let newConnString = new DbConnectionStringBuilder();
    newConnString.ConnectionString <- connString
    newConnString.Item("data source") <- ip
    
    log ("New App.config connString: " + Environment.NewLine + "\t" + newConnString.ToString())

    updateConnectionString "TestDb" (newConnString.ToString()) appConfig
    CopyFile "src/Akka.Persistence.SqlServer.Tests/bin/Release/Akka.Persistence.SqlServer.Tests.dll.config" appConfig

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

module Nuget = 
    // add Akka dependency for other projects
    let getAkkaDependency project =
        match project with
        | _ -> []

     // used to add -pre suffix to pre-release packages
    let getProjectVersion project =
      match project with
      | "Akka.Cluster" -> preReleaseVersion
      | persistence when persistence.StartsWith("Akka.Persistence") -> preReleaseVersion
      | _ -> release.NugetVersion

open Nuget
open NuGet.Update

//--------------------------------------------------------------------------------
// Upgrade nuget package versions for dev and production

let updateNugetPackages _ =
  printfn "Updating NuGet dependencies"

  let getConfigFile preRelease =
    match preRelease with
    | true -> "src/.nuget/NuGet.Dev.Config" 
    | false -> "src/.nuget/NuGet.Config" 

  let getPackages project =
    match project with
    | "Akka.Persistence.SqlServer" -> ["Akka.Persistence";"Akka.Persistence.Sql.Common"]
    | "Akka.Persistence.SqlServer.Tests" -> ["Akka.Persistence.TestKit";"Akka.Persistence.Sql.Common";]
    | _ -> []

  for projectFile in !! "src/**/*.csproj" do
    printfn "Updating packages for %s" projectFile
    let project = Path.GetFileNameWithoutExtension projectFile
    let projectDir = Path.GetDirectoryName projectFile
    let config = projectDir @@ "packages.config"

    NugetUpdate
        (fun p ->
                { p with
                    ConfigFile = Some (getConfigFile isPreRelease)
                    Prerelease = true
                    ToolPath = nugetExe
                    RepositoryPath = "src/Packages"
                    Ids = getPackages project
                    }) config

Target "UpdateDependencies" <| fun _ ->
    printfn "Invoking updateNugetPackages"
    updateNugetPackages()

//--------------------------------------------------------------------------------
// Clean nuget directory

Target "CleanNuget" <| fun _ ->
    CleanDir nugetDir

//--------------------------------------------------------------------------------
// Pack nuget for all projects
// Publish to nuget.org if nugetkey is specified

let createNugetPackages _ =
    let removeDir dir = 
        let del _ = 
            DeleteDir dir
            not (directoryExists dir)
        runWithRetries del 3 |> ignore

    ensureDirectory nugetDir
    for nuspec in !! "src/**/*.nuspec" do
        printfn "Creating nuget packages for %s" nuspec
        
        CleanDir workingDir

        let project = Path.GetFileNameWithoutExtension nuspec 
        let projectDir = Path.GetDirectoryName nuspec
        let projectFile = (!! (projectDir @@ project + ".*sproj")) |> Seq.head
        let releaseDir = projectDir @@ @"bin\Release"
        let packages = projectDir @@ "packages.config"
        let packageDependencies = if (fileExists packages) then (getDependencies packages) else []
        let dependencies = packageDependencies @ getAkkaDependency project
        let releaseVersion = getProjectVersion project

        let pack outputDir symbolPackage =
            NuGetHelper.NuGet
                (fun p ->
                    { p with
                        Description = description
                        Authors = authors
                        Copyright = copyright
                        Project =  project
                        Properties = ["Configuration", "Release"]
                        ReleaseNotes = release.Notes |> String.concat "\n"
                        Version = releaseVersion
                        Tags = tags |> String.concat " "
                        OutputPath = outputDir
                        WorkingDir = workingDir
                        SymbolPackage = symbolPackage
                        Dependencies = dependencies })
                nuspec

        // Copy dll, pdb and xml to libdir = workingDir/lib/net45/
        ensureDirectory libDir
        !! (releaseDir @@ project + ".dll")
        ++ (releaseDir @@ project + ".pdb")
        ++ (releaseDir @@ project + ".xml")
        ++ (releaseDir @@ project + ".ExternalAnnotations.xml")
        |> CopyFiles libDir

        // Copy all src-files (.cs and .fs files) to workingDir/src
        let nugetSrcDir = workingDir @@ @"src/"
        // CreateDir nugetSrcDir

        let isCs = hasExt ".cs"
        let isFs = hasExt ".fs"
        let isAssemblyInfo f = (filename f).Contains("AssemblyInfo")
        let isSrc f = (isCs f || isFs f) && not (isAssemblyInfo f) 
        CopyDir nugetSrcDir projectDir isSrc
        
        //Remove workingDir/src/obj and workingDir/src/bin
        removeDir (nugetSrcDir @@ "obj")
        removeDir (nugetSrcDir @@ "bin")

        // Create both normal nuget package and symbols nuget package. 
        // Uses the files we copied to workingDir and outputs to nugetdir
        pack nugetDir NugetSymbolPackage.Nuspec


let publishNugetPackages _ = 
    let rec publishPackage url accessKey trialsLeft packageFile =
        let tracing = enableProcessTracing
        enableProcessTracing <- false
        let args p =
            match p with
            | (pack, key, "") -> sprintf "push \"%s\" %s" pack key
            | (pack, key, url) -> sprintf "push \"%s\" %s -source %s" pack key url

        tracefn "Pushing %s Attempts left: %d" (FullName packageFile) trialsLeft
        try 
            let result = ExecProcess (fun info -> 
                    info.FileName <- nugetExe
                    info.WorkingDirectory <- (Path.GetDirectoryName (FullName packageFile))
                    info.Arguments <- args (packageFile, accessKey,url)) (System.TimeSpan.FromMinutes 1.0)
            enableProcessTracing <- tracing
            if result <> 0 then failwithf "Error during NuGet symbol push. %s %s" nugetExe (args (packageFile, accessKey,url))
        with exn -> 
            if (trialsLeft > 0) then (publishPackage url accessKey (trialsLeft-1) packageFile)
            else raise exn
    let shouldPushNugetPackages = hasBuildParam "nugetkey"
    let shouldPushSymbolsPackages = (hasBuildParam "symbolspublishurl") && (hasBuildParam "symbolskey")
    
    if (shouldPushNugetPackages || shouldPushSymbolsPackages) then
        printfn "Pushing nuget packages"
        if shouldPushNugetPackages then
            let normalPackages= 
                !! (nugetDir @@ "*.nupkg") 
                -- (nugetDir @@ "*.symbols.nupkg") |> Seq.sortBy(fun x -> x.ToLower())
            for package in normalPackages do
                publishPackage (getBuildParamOrDefault "nugetpublishurl" "") (getBuildParam "nugetkey") 3 package

        if shouldPushSymbolsPackages then
            let symbolPackages= !! (nugetDir @@ "*.symbols.nupkg") |> Seq.sortBy(fun x -> x.ToLower())
            for package in symbolPackages do
                publishPackage (getBuildParam "symbolspublishurl") (getBuildParam "symbolskey") 3 package


Target "Nuget" <| fun _ -> 
    createNugetPackages()
    publishNugetPackages()

Target "CreateNuget" <| fun _ -> 
    createNugetPackages()

Target "PublishNuget" <| fun _ -> 
    publishNugetPackages()



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
"Clean" ==> "AssemblyInfo" ==> "RestorePackages" ==> "UpdateDependencies" ==> "Build" ==> "CopyOutput" ==> "BuildRelease"

// tests dependencies
"CleanTests" ==> "RunTests"

// tests with docker dependencies
Target "RunTestsWithDocker" DoNothing
"CleanTests" ==> "ActivateFinalTargets" ==> "StartDbContainer" ==> "PrepAppConfig" ==> "RunTests" ==> "RunTestsWithDocker"

// nuget dependencies
"CleanNuget" ==> "CreateNuget"
"CleanNuget" ==> "BuildRelease" ==> "Nuget"

Target "All" DoNothing
"BuildRelease" ==> "All"
"RunTests" ==> "All"
"Nuget" ==> "All"

Target "AllWithDockerTests" DoNothing
"BuildRelease" ==> "AllWithDockerTests"
"RunTestsWithDocker" ==> "AllWithDockerTests"
"Nuget" ==> "AllWithDockerTests"

RunTargetOrDefault "Help"

