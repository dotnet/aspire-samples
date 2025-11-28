using System.Runtime.InteropServices;
using CrossPlatform.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var setup = new AspireSetup(builder);
var resources = setup.Initialise();
    
if (OperatingSystem.IsMacOS() && RuntimeInformation.OSArchitecture == Architecture.Arm64)
{
    setup
        .DoMacSetup(resources)
        .ThenWireUpTargets();

}
else if (OperatingSystem.IsWindows() && RuntimeInformation.OSArchitecture == Architecture.X64)
{
    setup
        .DoWindowsSetup(resources)
        .ThenWireUpTargets();
}
else
{
    Console.WriteLine("¯\\_(ツ)_/¯ are you running on a potato...?");
}

builder.Build().Run();
Console.WriteLine("Exit");
