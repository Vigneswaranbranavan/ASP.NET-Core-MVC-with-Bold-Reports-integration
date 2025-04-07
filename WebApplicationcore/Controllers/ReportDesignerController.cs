using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using BoldReports.Web.ReportDesigner;
using System.Collections.Generic;
using System.IO;
using WebApplicationcore.Models;
using BoldReports.Web.ReportViewer;


[Route("api/[controller]/[action]")]
[Microsoft.AspNetCore.Cors.EnableCors("AllowAllOrigins")]
[ApiController]
public class ReportDesignerController : Controller, IReportDesignerController
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IMemoryCache _cache;
    private const string ResourceFolder = "Resources";

    public ReportDesignerController(IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache)
    {
        _hostingEnvironment = hostingEnvironment;
        _cache = memoryCache;
    }

    #region Report Designer Endpoints

    [HttpPost]
    public object PostDesignerAction([FromBody] Dictionary<string, object> jsonResult)
    {
        return ReportDesignerHelper.ProcessDesigner(jsonResult, this, null, _cache);
    }


    [HttpPost]
    public object PostFormDesignerAction()
    {
        return ReportDesignerHelper.ProcessDesigner(null, this, null, _cache);
    }

    [HttpPost]
    public void UploadReportAction()
    {
        if (Request.Form.Files.Count == 0)
            return;

        var file = Request.Form.Files[0];
        string uploadPath = GetResourcePath();

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        string filePath = Path.Combine(uploadPath, file.FileName);
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(fileStream);
        }
    }

    //[HttpPost]
    //public object GetDataSourceItems([FromBody] Dictionary<string, object> jsonData)
    //{
    //    // Only allow Microsoft SQL and Shared data sources
    //    var dataSources = new List<CustomDataSourceItem>
    //{
    //    new CustomDataSourceItem
    //    {
    //        Name = "Microsoft SQL Server",
    //        Type = "SQL"
    //    },
    //    new CustomDataSourceItem
    //    {
    //        Name = "Shared Data Source",
    //        Type = "Shared"
    //    }
    //};

    //    return dataSources;
    //}


    [HttpGet]
    public object GetImage(string key, string image)
    {
        string imagePath = Path.Combine(GetResourcePath(), image);

        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound();
        }

        byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
        return File(imageBytes, "image/png");
    }

    #endregion

    #region Report Management

    [HttpPost]
    public IActionResult SaveReport([FromBody] ReportSaveModel reportData)
    {
        if (string.IsNullOrEmpty(reportData.ReportName))
        {
            return BadRequest("Report name cannot be empty");
        }

        string reportPath = Path.Combine(GetResourcePath(), reportData.ReportName + ".rdl");
        System.IO.File.WriteAllText(reportPath, reportData.ReportContent);
        return Ok(new { Message = "Report saved successfully" });
    }

    [HttpGet]
    public IActionResult GetReports()
    {
        string resourcePath = GetResourcePath();

        if (!Directory.Exists(resourcePath))
        {
            return Ok(new List<string>()); // Return empty list instead of NotFound
        }

        var reportFiles = Directory.GetFiles(resourcePath, "*.rdl")
                                .Select(Path.GetFileNameWithoutExtension)
                                .ToList();

        return Ok(reportFiles);
    }

    [HttpDelete("{reportName}")]
    public IActionResult DeleteReport(string reportName)
    {
        string reportPath = Path.Combine(GetResourcePath(), reportName + ".rdl");

        if (System.IO.File.Exists(reportPath))
        {
            System.IO.File.Delete(reportPath);
            return Ok(new { Message = "Report deleted successfully" });
        }

        return NotFound("Report not found");
    }

    #endregion

    public object PostReportAction(Dictionary<string, object> jsonResult)
    {
        // 1. Verify report exists
        if (jsonResult.ContainsKey("reportPath"))
        {
            var reportPath = Path.Combine(GetResourcePath(), jsonResult["reportPath"].ToString() + ".rdl");
            if (!System.IO.File.Exists(reportPath))
            {
                Response.StatusCode = 404;
                return new { error = "Report not found" };
            }
        }

        // 2. Process with ReportHelper
        try
        {
            return ReportHelper.ProcessReport(jsonResult, this, _cache);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;
            return new
            {
                error = "Preview failed",
                details = ex.Message
            };
        }
    }

    public void OnInitReportOptions(ReportViewerOptions reportOption)
    {
        if (string.IsNullOrEmpty(reportOption.ReportModel.ReportPath))
            return;

        var reportPath = Path.Combine(GetResourcePath(), reportOption.ReportModel.ReportPath + ".rdl");

        if (System.IO.File.Exists(reportPath))
        {
            using var fileStream = new FileStream(reportPath, FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            reportOption.ReportModel.Stream = memoryStream;
        }
    }


    #region Required Interface Implementations

    public bool SetData(string key, string itemId, ItemInfo itemData, out string errMsg)
    {
        errMsg = string.Empty;
        return true;
    }

    public ResourceInfo GetData(string key, string itemId)
    {
        return new ResourceInfo();
    }

    public object PostFormReportAction()
    {
        var viewerController = new ReportViewerController(_cache, _hostingEnvironment);
        return viewerController.PostFormReportAction();
    }

  

    public void OnReportLoaded(ReportViewerOptions reportOption)
    {
        // Handle report loaded event
    }

   
    public object GetResource(ReportResource resource)
    {
        // Basic implementation without ReportViewerHelper
        return new { };
    }

    #endregion

    #region Helper Methods

    private string GetResourcePath()
    {
        return Path.Combine(_hostingEnvironment.WebRootPath, ResourceFolder);
    }

    //private string GetReportPath(string reportName)
    //{
    //    return Path.Combine(GetResourcePath(), reportName + ".rdl");
    //}

    #endregion


}