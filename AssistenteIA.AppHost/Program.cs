var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var servico1 = builder.AddProject<Projects.AssistenteIA_Servico1>("servico1").WithExternalHttpEndpoints();
var servico2 = builder.AddProject<Projects.AssistenteIA_Servico2>("servico2").WithExternalHttpEndpoints();


var apiService = builder.AddProject<Projects.AssistenteIA_ApiService>("apiservice")
    .WithReference(servico1)
    .WithReference(servico2);

builder.AddProject<Projects.AssistenteIA_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
