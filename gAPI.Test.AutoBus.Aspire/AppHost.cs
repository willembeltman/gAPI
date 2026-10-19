var builder = DistributedApplication.CreateBuilder(args);

// 1. Start een RabbitMQ container
var rabbitMq = builder.AddRabbitMQ("ServiceBus")
                      .WithManagementPlugin(); // Optioneel: activeert de RabbitMQ web-dashboard

// 2. Definieer de 3 console applicaties en koppel RabbitMQ
var mainApp = builder.AddProject<Projects.gAPI_Test_AutoBus_MainApp>("mainapp")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithEnvironment("QueueName", "Standard");

var microService1 = builder.AddProject<Projects.gAPI_Test_AutoBus_MicroService1>("microservice1")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithEnvironment("QueueName", "Standard");

var microService2 = builder.AddProject<Projects.gAPI_Test_AutoBus_Microservice2>("microservice2")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithEnvironment("QueueName", "Standard");

builder.Build().Run();
