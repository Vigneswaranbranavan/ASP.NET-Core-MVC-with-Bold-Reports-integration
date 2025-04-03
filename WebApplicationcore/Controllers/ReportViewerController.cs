using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;
using BoldReports.AspNetCore;
using System.IO;
using System.Collections.Generic;
using BoldReports.Writer;
using BoldReports.Web.ReportViewer;



[Route("api/[controller]/[action]")]
[Microsoft.AspNetCore.Cors.EnableCors("AllowAllOrigins")]
[ApiController]
public class ReportViewerController : Controller, IReportController
{
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private const string ResourceFolder = "Resources";

    public ReportViewerController(IMemoryCache memoryCache, IWebHostEnvironment hostingEnvironment)
    {
        _cache = memoryCache;
        _hostingEnvironment = hostingEnvironment;
    }

    [HttpPost]
    public object PostReportAction([FromBody] Dictionary<string, object> jsonArray)
    {
        return ReportHelper.ProcessReport(jsonArray, this, _cache);
    }


    [NonAction]
    public void OnInitReportOptions(ReportViewerOptions reportOption)
    {
        string reportPath = Path.Combine(GetResourcePath(), reportOption.ReportModel.ReportPath + ".rdl");

        if (!System.IO.File.Exists(reportPath))
        {
            throw new FileNotFoundException("Report file not found: " + reportPath);
        }

        using FileStream inputStream = new FileStream(reportPath, FileMode.Open, FileAccess.Read);
        MemoryStream reportStream = new MemoryStream();
        inputStream.CopyTo(reportStream);
        reportStream.Position = 0;
        reportOption.ReportModel.Stream = reportStream;
    }

    [NonAction]
    public void OnReportLoaded(ReportViewerOptions reportOption)
    {
        // Additional processing when report is loaded
    }

    [HttpGet]
    [ActionName("GetResource")]
    public object GetResource(ReportResource resource)
    {
        return ReportHelper.GetResource(resource, this, _cache);
    }

    [HttpPost]
    public object PostFormReportAction()
    {
        try
        {
            // Get the form data from the request
            var formData = Request.Form;

            // Convert form data to dictionary for processing
            var jsonResult = new Dictionary<string, object>();
            foreach (var key in formData.Keys)
            {
                jsonResult.Add(key, formData[key]);
            }

            var result = ReportHelper.ProcessReport(jsonResult, this, _cache);

            // Handle export requests
            if (formData.ContainsKey("exportType"))
            {
                string exportType = formData["exportType"];

                if (result is Dictionary<string, object> resultDict &&
                    resultDict.TryGetValue("result", out var exportObj) &&
                    exportObj is byte[] exportData)
                {
                    // For file exports, we need to modify the response directly
                    Response.ContentType = GetMimeType(exportType);
                    Response.Headers.Add("Content-Disposition", $"attachment; filename=ReportExport.{exportType.ToLower()}");
                    Response.Body.Write(exportData, 0, exportData.Length);
                    return null; // Return null since we're handling the response directly
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            // Return error as object to match interface
            return new
            {
                error = "Export failed",
                details = ex.Message
            };
        }
    }

    private string GetMimeType(string exportType)
    {
        return exportType.ToLower() switch
        {
            "pdf" => "application/pdf",
            "excel" => "application/vnd.ms-excel",
            "word" => "application/msword",
            "html" => "text/html",
            "csv" => "text/csv",
            "ppt" => "application/vnd.ms-powerpoint",
            _ => "application/octet-stream"
        };
    }

    private string GetResourcePath()
    {
        return Path.Combine(_hostingEnvironment.WebRootPath, ResourceFolder);
    }
}