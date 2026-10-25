var builder = DistributedApplication.CreateBuilder(args);

// OPTIE 1: Fabric (gAPI Fabric Server)
var backplane = builder.AddProject<Projects.gAPI_Test_Fabric>("backplane")
    .WithEnvironment("Port", "9494")
    .WithEnvironment("BusPort", "9595");

var storage = builder.AddProject<Projects.gAPI_Test_Storage>("storage")
    .WithEnvironment("LocalStorageServerConfig__SuperSecretKey", "SUPERSECRETKEYOF32CHARACTERSLONG")
    .WithEnvironment("LocalStorageServerConfig__Credentials__UserName", "USERNAME")
    .WithEnvironment("LocalStorageServerConfig__Credentials__Password", "PASSWORD");

var mainServer = builder.AddProject<Projects.gAPI_Test_Main_Server>("mainserver")
    .WithReference(backplane)
    .WithReference(storage)
    .WaitFor(backplane)
    .WaitFor(storage)
    .WithEnvironment("FrontendUrl", "IsApp_SoNoCors")
    .WithEnvironment("UseMemoryDatabase", "true")
    .WithEnvironment("QueueName", "Standard")
    .WithEnvironment("ConnectionStrings__DefaultConnection", "UsingMemoryDatabase_SoNoConnectionstring")
    .WithEnvironment("ConnectionStrings__StorageConnection", "Provider=StorageServer;Server=https://localhost:5555;Username=USERNAME;Password=PASSWORD;UrlTimeout=1;AuthenticateTimeout=15")
    .WithEnvironment("ConnectionStrings__FabricConnection", "Server=localhost;Port=9494")
    .WithEnvironment("ConnectionStrings__ServiceBus", "Server=localhost;Port=9595");

var mainClient = builder.AddProject<Projects.gAPI_Test_Main_Client>("mainclient")
    .WithReference(mainServer)
    .WaitFor(mainServer)
    .WithEnvironment("ApiBackendUrl", "https://localhost:7048")
    .WithEnvironment("WssBackendUrl", "wss://127.0.0.1:7048");

var microService1 = builder.AddProject<Projects.gAPI_Test_MicroService1>("microservice1")
    .WithReference(backplane)
    .WaitFor(backplane)
    .WithEnvironment("ConnectionStrings__ServiceBus", "Server=localhost;Port=9595")
    .WithEnvironment("QueueName", "Standard");

var microService2 = builder.AddProject<Projects.gAPI_Test_MicroService2>("microservice2")
    .WithReference(backplane)
    .WaitFor(backplane)
    .WithEnvironment("ConnectionStrings__ServiceBus", "Server=localhost;Port=9595")
    .WithEnvironment("QueueName", "Standard");

builder.AddProject<Projects.gAPI_Test_Storage>("gapi-test-storage");

builder.Build().Run();

//// OPTIE 2: RabbitMQ
//var rabbitMq = builder.AddRabbitMQ("ServiceBus")
//                      .WithManagementPlugin();
//var mainApp = builder.AddProject<Projects.gAPI_Test_MainApp>("mainapp")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//var microService1 = builder.AddProject<Projects.gAPI_Test_MicroService1>("microservice1")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//var microService2 = builder.AddProject<Projects.gAPI_Test_MicroService2>("microservice2")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//builder.Build().Run();

//// 1. Voeg Azure Service Bus toe én dwing hem om lokaal als emulator te draaien.
//// Dit regelt de juiste Microsoft-images, poorten en SQL Server volledig automatisch!
//var azureServiceBus = builder.AddAzureServiceBus("ServiceBus")
//                             .RunAsEmulator();

//// 2. Koppel de applicaties. Aspire injecteert nu AUTOMATISCH de juiste lokale connectiestring.
//var mainApp = builder.AddProject<Projects.gAPI_Test_MainApp>("mainapp")
//    .WithReference(azureServiceBus)
//    .WaitFor(azureServiceBus)
//    .WithEnvironment("QueueName", "Standard");

//var microService1 = builder.AddProject<Projects.gAPI_Test_MicroService1>("microservice1")
//    .WithReference(azureServiceBus)
//    .WaitFor(azureServiceBus)
//    .WithEnvironment("QueueName", "Standard");

//var microService2 = builder.AddProject<Projects.gAPI_Test_MicroService2>("microservice2")
//    .WithReference(azureServiceBus)
//    .WaitFor(azureServiceBus)
//    .WithEnvironment("QueueName", "Standard");

//builder.Build().Run();


