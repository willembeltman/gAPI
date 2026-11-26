var builder = DistributedApplication.CreateBuilder(args);

// OPTIE 1: Fabric (gAPI Fabric Server)
var backplane = builder.AddProject<Projects.gAPI_Example_Fabric>("backplane")
    .WithEnvironment("Port", "9494")
    .WithEnvironment("PlanePort", "9696");

var storage = builder.AddProject<Projects.gAPI_Example_Storage>("storage")
    .WithEnvironment("BaseUrl", "https://localhost:7275")
    .WithEnvironment("ConnectionStrings__BackPlane", "Server=localhost;Port=9696");

var apiServer = builder.AddProject<Projects.gAPI_Example_ApiServer>("apiserver")
    .WithReference(backplane)
    .WithReference(storage)
    .WaitFor(backplane)
    .WaitFor(storage)
    .WithEnvironment("FrontendUrl", "IsApp_SoNoCors")
    .WithEnvironment("UseMemoryDatabase", "true")
    .WithEnvironment("QueueName", "Standard")
    .WithEnvironment("ConnectionStrings__DefaultConnection", "UsingMemoryDatabase_SoNoConnectionstring")
    .WithEnvironment("ConnectionStrings__StorageConnection", "Provider=StorageServer;UrlTimeout=1;AuthenticateTimeout=15")
    .WithEnvironment("ConnectionStrings__FabricConnection", "Server=localhost;Port=9494")
    .WithEnvironment("ConnectionStrings__BackPlane", "Server=localhost;Port=9696");

var apiClient = builder.AddProject<Projects.gAPI_Example_ApiClient>("apiclient")
    .WithReference(apiServer)
    .WaitFor(apiServer)
    .WithEnvironment("ApiBackendUrl", "https://localhost:7048")
    .WithEnvironment("WssBackendUrl", "wss://127.0.0.1:7048");

var wssServer = builder.AddProject<Projects.gAPI_Example_WssServer>("wssserver")
    .WithReference(backplane)
    .WithReference(storage)
    .WaitFor(backplane)
    .WaitFor(storage)
    .WithEnvironment("FrontendUrl", "IsApp_SoNoCors")
    .WithEnvironment("UseMemoryDatabase", "true")
    .WithEnvironment("QueueName", "Standard")
    .WithEnvironment("ConnectionStrings__DefaultConnection", "UsingMemoryDatabase_SoNoConnectionstring")
    .WithEnvironment("ConnectionStrings__StorageConnection", "Provider=StorageServer;UrlTimeout=1;AuthenticateTimeout=15")
    .WithEnvironment("ConnectionStrings__FabricConnection", "Server=localhost;Port=9494")
    .WithEnvironment("ConnectionStrings__BackPlane", "Server=localhost;Port=9696");

var wssClient = builder.AddProject<Projects.gAPI_Example_WssClient>("wssclient")
    .WithReference(wssServer)
    .WaitFor(wssServer)
    .WithEnvironment("ApiBackendUrl", "https://localhost:7049")
    .WithEnvironment("WssBackendUrl", "wss://127.0.0.1:7049");

var microService1 = builder.AddProject<Projects.gAPI_Example_MicroService1>("microservice1")
    .WithReference(backplane)
    .WaitFor(backplane)
    .WithEnvironment("ConnectionStrings__BackPlane", "Server=localhost;Port=9696")
    .WithEnvironment("QueueName", "Standard");

var microService2 = builder.AddProject<Projects.gAPI_Example_Microservice2>("microservice2")
    .WithReference(backplane)
    .WaitFor(backplane)
    .WithEnvironment("ConnectionStrings__BackPlane", "Server=localhost;Port=9696")
    .WithEnvironment("QueueName", "Standard");

builder.AddProject<Projects.gAPI_Example_Storage>("gapi-test-storage");

builder.Build().Run();

//// OPTIE 2: RabbitMQ
//var rabbitMq = builder.AddRabbitMQ("BackPlane")
//                      .WithManagementPlugin();
//var mainApp = builder.AddProject<Projects.gAPI_Example_MainApp>("mainapp")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//var microService1 = builder.AddProject<Projects.gAPI_Example_MicroService1>("microservice1")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//var microService2 = builder.AddProject<Projects.gAPI_Example_MicroService2>("microservice2")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//builder.Build().Run();


//// OPTIE 3: Azure Service Plane
//// 1. Voeg Azure Service Plane toe én dwing hem om lokaal als emulator te draaien.
//// Dit regelt de juiste Microsoft-images, poorten en SQL Server volledig automatisch!
//var AzureServicePlane = builder.AddAzureServicePlane("BackPlane")
//                             .RunAsEmulator();

//// 2. Koppel de applicaties. Aspire injecteert nu AUTOMATISCH de juiste lokale connectiestring.
//var mainApp = builder.AddProject<Projects.gAPI_Example_MainApp>("mainapp")
//    .WithReference(AzureServicePlane)
//    .WaitFor(AzureServicePlane)
//    .WithEnvironment("QueueName", "Standard");

//var microService1 = builder.AddProject<Projects.gAPI_Example_MicroService1>("microservice1")
//    .WithReference(AzureServicePlane)
//    .WaitFor(AzureServicePlane)
//    .WithEnvironment("QueueName", "Standard");

//var microService2 = builder.AddProject<Projects.gAPI_Example_MicroService2>("microservice2")
//    .WithReference(AzureServicePlane)
//    .WaitFor(AzureServicePlane)
//    .WithEnvironment("QueueName", "Standard");

//builder.Build().Run();


