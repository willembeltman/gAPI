var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.gAPI_Example_Fabric>("backplane")
    .WithEnvironment("Port", "9494")
    .WithEnvironment("PlanePort", "9595");

builder.Build().Run();
