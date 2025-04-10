using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BlobZipDownloader.Models;
using Azure.Storage.Blobs;
using System.IO.Compression;

namespace BlobZipDownloader.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public async Task<IActionResult> DownloadZip(string connectionString, string folderPath)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempFolder);

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient("claims"); // ajuste conforme necessário

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: folderPath))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var localFilePath = Path.Combine(tempFolder, blobItem.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath)!);

            await using var fileStream = System.IO.File.OpenWrite(localFilePath);
            await blobClient.DownloadToAsync(fileStream);
        }

        var zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        ZipFile.CreateFromDirectory(tempFolder, zipPath);

        var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);

        Directory.Delete(tempFolder, true);
        System.IO.File.Delete(zipPath);

        return File(zipBytes, "application/zip", "download.zip");
    }
}
