using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Models;
using OlapWebBTL.Services;

namespace OlapWebBTL.Controllers;

public class LuuKhoController : Controller
{
    private readonly RolapService _rolap;
    public LuuKhoController(RolapService rolap) => _rolap = rolap;

    public IActionResult Index(
        string? gTime = null, string? gObj = null, string? gProd = null)
    {
        var filters = new Dictionary<string, List<string>>();
        foreach (var kv in Request.Query)
        {
            if (!kv.Key.StartsWith("f_", StringComparison.OrdinalIgnoreCase)) continue;
            var col = kv.Key.Substring(2);
            var vals = kv.Value.Where(v => !string.IsNullOrWhiteSpace(v))
                               .Select(v => v!.Trim()).ToList();
            if (vals.Count > 0) filters[col] = vals;
        }

        var timeNeeded = new HashSet<string>();
        foreach (var c in new[] { "Nam", "Quy", "Thang", "Ngay" })
            if (filters.ContainsKey(c)) timeNeeded.Add(c);
        if (!string.IsNullOrEmpty(gTime)) timeNeeded.Add(gTime);
        if (timeNeeded.Contains("Thang") || timeNeeded.Contains("Ngay"))
            timeNeeded.Remove("Quy");

        var prodNeeded = new HashSet<string>();
        if (filters.ContainsKey("Ma_MH")) prodNeeded.Add("Ma_MH");
        if (gProd == "Ma_MH") prodNeeded.Add("Ma_MH");

        var storeNeeded = new HashSet<string>();
        foreach (var c in new[] { "Bang", "Ten_thanh_pho", "Ma_CH" })
            if (filters.ContainsKey(c)) storeNeeded.Add(c);
        if (!string.IsNullOrEmpty(gObj)) storeNeeded.Add(gObj);

        var t = CuboidMap.PickLevel(CuboidMap.TimeCols, timeNeeded);
        var p = CuboidMap.PickLevel(CuboidMap.ProductCols, prodNeeded);
        var s2 = CuboidMap.PickLevel(CuboidMap.StoreCols, storeNeeded);

        var q = new OlapQuery
        {
            Cube = "LK", T = t, P = p, S = s2, Filters = filters, MeasureForChart = "TongTon",
        };
        var result = _rolap.QueryLuuKho(t, p, s2, q);

        var meta = new DashboardMeta
        {
            CubeCode = "LK", CubeName = "LƯU KHO",
            ObjectDimCode = "S", ObjectDimName = "Cửa hàng",
        };
        foreach (var l in CuboidMap.TimeLevels) meta.TimeLevels[l] = CuboidMap.TimeLabel[l];
        foreach (var l in CuboidMap.ProductLevels) meta.ProductLevels[l] = CuboidMap.ProductLabel[l];
        foreach (var l in CuboidMap.StoreLevels) meta.ObjectLevels[l] = CuboidMap.StoreLabel[l];

        var vm = new OlapDashboardVM
        {
            Query = q, Result = result, Meta = meta,
            Options = _rolap.GetFilterOptions(),
            GroupTime = gTime ?? "", GroupObject = gObj ?? "", GroupProduct = gProd ?? "",
        };
        return View(vm);
    }
}
