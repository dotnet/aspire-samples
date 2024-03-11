using HolisticWare.Tools.Aspire.Hosting.Clients.Python;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireWithPython_ScriptsAndWebDjangoFlask_ApiService>("apiservice");

builder.AddProject<Projects.AspireWithPython_ScriptsAndWebDjangoFlask_Web>("webfrontend")
    .WithReference(apiService);

builder.AddScriptPython
    (
        "clients-python-machine-learning",
        $"{Environment.GetEnvironmentVariable("HOME")}/moljac-python/venv/bin/python3",
        "../Clients/Python/simple-machine-learning/",
        new string[] { "test1.py"}
    )
    .WithReference(apiService);

builder.AddScriptPythonDjango
    (
        "webapp-python-django-web-frontent",
        $"{Environment.GetEnvironmentVariable("HOME")}/moljac-python/venv/bin/python3",
        "../Clients/Python/django/AspireTest/",
        new string[] { "manage.py", "runserver" }
    )
    .WithReference(apiService);

builder.AddScriptPythonFlask
    (
        "webapp-python-flask-web-frontent",
        $".venv/bin/flask",
        //$"flask",
        "../Clients/Python/flask/",
        new string[] { "--app", "app-minimal.py", "run" }
    )
    .WithReference(apiService);

builder.Build().Run();
