var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.gAPI_Test_Fabric>("backplane")
    .WithEnvironment("Port", "9494")
    .WithEnvironment("BusPort", "9595");

builder.Build().Run();
