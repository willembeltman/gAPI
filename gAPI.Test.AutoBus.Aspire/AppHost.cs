var builder = DistributedApplication.CreateBuilder(args);

// OPTIE 1: Fabric (gAPI Fabric Server)
var fabric = builder.AddProject<Projects.gAPI_Test_AutoBus_Fabric>("fabric");

var mainApp = builder.AddProject<Projects.gAPI_Test_AutoBus_MainApp>("mainapp")
    .WithReference(fabric)
    .WaitFor(fabric)
    .WithEnvironment("ConnectionStrings__ServiceBus", "Server=localhost;Port=9494")
    .WithEnvironment("QueueName", "Standard");

var microService1 = builder.AddProject<Projects.gAPI_Test_AutoBus_MicroService1>("microservice1")
    .WithReference(fabric)
    .WaitFor(fabric)
    .WithEnvironment("ConnectionStrings__ServiceBus", "Server=localhost;Port=9494")
    .WithEnvironment("QueueName", "Standard");

var microService2 = builder.AddProject<Projects.gAPI_Test_AutoBus_Microservice2>("microservice2")
    .WithReference(fabric)
    .WaitFor(fabric)
    .WithEnvironment("ConnectionStrings__ServiceBus", "Server=localhost;Port=9494")
    .WithEnvironment("QueueName", "Standard");

builder.Build().Run();

//// OPTIE 2: RabbitMQ
//var rabbitMq = builder.AddRabbitMQ("ServiceBus")
//                      .WithManagementPlugin();
//var mainApp = builder.AddProject<Projects.gAPI_Test_AutoBus_MainApp>("mainapp")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//var microService1 = builder.AddProject<Projects.gAPI_Test_AutoBus_MicroService1>("microservice1")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//var microService2 = builder.AddProject<Projects.gAPI_Test_AutoBus_Microservice2>("microservice2")
//    .WithReference(rabbitMq)
//    .WaitFor(rabbitMq)
//    .WithEnvironment("QueueName", "Standard");
//builder.Build().Run();

//// 1. Voeg Azure Service Bus toe én dwing hem om lokaal als emulator te draaien.
//// Dit regelt de juiste Microsoft-images, poorten en SQL Server volledig automatisch!
//var azureServiceBus = builder.AddAzureServiceBus("ServiceBus")
//                             .RunAsEmulator();

//// 2. Koppel de applicaties. Aspire injecteert nu AUTOMATISCH de juiste lokale connectiestring.
//var mainApp = builder.AddProject<Projects.gAPI_Test_AutoBus_MainApp>("mainapp")
//    .WithReference(azureServiceBus)
//    .WaitFor(azureServiceBus)
//    .WithEnvironment("QueueName", "Standard");

//var microService1 = builder.AddProject<Projects.gAPI_Test_AutoBus_MicroService1>("microservice1")
//    .WithReference(azureServiceBus)
//    .WaitFor(azureServiceBus)
//    .WithEnvironment("QueueName", "Standard");

//var microService2 = builder.AddProject<Projects.gAPI_Test_AutoBus_Microservice2>("microservice2")
//    .WithReference(azureServiceBus)
//    .WaitFor(azureServiceBus)
//    .WithEnvironment("QueueName", "Standard");

//builder.Build().Run();


