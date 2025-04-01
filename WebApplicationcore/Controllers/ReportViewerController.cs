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

public class ReportViewerController : Controller, IReportController
{
    // Report viewer requires a memory cache to store the information of consecutive client request and
    // have the rendered report viewer information in server.
    private Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    // IWebHostEnvironment used with sample to get the application data from wwwroot.
    private Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;

    // Post action to process the report from server based json parameters and send the result back to the client.
    public ReportViewerController(Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache,
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment)
    {
        _cache = memoryCache;
        _hostingEnvironment = hostingEnvironment;
    }

    // Post action to process the report from server based json parameters and send the result back to the client.
    [HttpPost]
    public object PostReportAction([FromBody] Dictionary<string, object> jsonArray)
    {
        return ReportHelper.ProcessReport(jsonArray, this, this._cache);
    }

    // Method will be called to initialize the report information to load the report with ReportHelper for processing.
    [NonAction]
    public void OnInitReportOptions(ReportViewerOptions reportOption)
    {
        string basePath = _hostingEnvironment.WebRootPath;
        string reportPath = Path.Combine(basePath, "Resources", reportOption.ReportModel.ReportPath);

        if (!System.IO.File.Exists(reportPath))
        {
            throw new FileNotFoundException("Report file not found: " + reportPath);
        }

        using (FileStream inputStream = new FileStream(reportPath, FileMode.Open, FileAccess.Read))
        {
            MemoryStream reportStream = new MemoryStream();
            inputStream.CopyTo(reportStream);
            reportStream.Position = 0;
            reportOption.ReportModel.Stream = reportStream;
        }
    }

    // Method will be called when reported is loaded with internally to start to layout process with ReportHelper.
    [NonAction]
    public void OnReportLoaded(ReportViewerOptions reportOption)
    {
    }

    //Get action for getting resources from the report
    [ActionName("GetResource")]
    [AcceptVerbs("GET")]
    // Method will be called from Report Viewer client to get the image src for Image report item.
    public object GetResource(ReportResource resource)
    {
        return ReportHelper.GetResource(resource, this, _cache);
    }

    [HttpPost]
    public object PostFormReportAction()
    {
        return ReportHelper.ProcessReport(null, this, _cache);
    }
}