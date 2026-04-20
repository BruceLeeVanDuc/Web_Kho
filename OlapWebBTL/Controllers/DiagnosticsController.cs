using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Services;

namespace OlapWebBTL.Controllers;

public class DiagnosticsController : Controller
{
    private readonly OlapService _olap;
    public DiagnosticsController(OlapService olap) => _olap = olap;

    public IActionResult Index()
    {
        var info = _olap.GetDiagnostics();
        return View(info);
    }

    [HttpPost]
    public IActionResult RunMdx(string mdx)
    {
        var result = _olap.ExecuteAsTable(mdx ?? "", "MDX tuỳ chỉnh");
        return View("MdxResult", result);
    }
}
