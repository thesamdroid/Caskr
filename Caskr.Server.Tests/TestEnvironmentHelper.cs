using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Caskr.Server.Tests;

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public TestWebHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        WebRootPath = contentRootPath;
    }

    public string ApplicationName { get; set; } = "Caskr.server";

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string WebRootPath { get; set; }

    public string EnvironmentName { get; set; } = "Development";

    public string ContentRootPath { get; set; }

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal static class TestEnvironmentHelper
{
    public static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Caskr.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Unable to locate solution root containing Caskr.sln");
        }

        return directory.FullName;
    }
}
