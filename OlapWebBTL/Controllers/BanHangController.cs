using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Models;
using OlapWebBTL.Services;

namespace OlapWebBTL.Controllers;

public class BanHangController : Controller
{
    private readonly RolapService _rolap;
    public BanHangController(RolapService rolap) => _rolap = rolap;

    public IActionResult Index(
        string? gTime = null, string? gObj = null, string? gProd = null)
    {
        // 1) Đọc filter từ query: f_Nam, f_Quy, f_Thang, f_Bang, f_Ten_thanh_pho, f_Loai_khach_hang, f_Ma_KH, f_Ma_MH
        var filters = new Dictionary<string, List<string>>();
        foreach (var kv in Request.Query)
        {
            if (!kv.Key.StartsWith("f_", StringComparison.OrdinalIgnoreCase)) continue;
            var col = kv.Key.Substring(2);
            var vals = kv.Value.Where(v => !string.IsNullOrWhiteSpace(v))
                               .Select(v => v!.Trim()).ToList();
            if (vals.Count > 0) filters[col] = vals;
        }

        // 2) Tính set cột cần thiết cho mỗi dim
        var timeNeeded = new HashSet<string>();
        foreach (var c in new[] { "Nam", "Quy", "Thang", "Ngay" })
            if (filters.ContainsKey(c)) timeNeeded.Add(c);
        if (!string.IsNullOrEmpty(gTime)) timeNeeded.Add(gTime);
        // Chuẩn hoá: Thang|Ngay át Quy (do T3/T4 không có Quy)
        if (timeNeeded.Contains("Thang") || timeNeeded.Contains("Ngay"))
            timeNeeded.Remove("Quy");

        var prodNeeded = new HashSet<string>();
        if (filters.ContainsKey("Ma_MH")) prodNeeded.Add("Ma_MH");
        if (gProd == "Ma_MH") prodNeeded.Add("Ma_MH");

        var custNeeded = new HashSet<string>();
        foreach (var c in new[] { "Bang", "Ten_thanh_pho", "Loai_khach_hang", "Ma_KH" })
            if (filters.ContainsKey(c)) custNeeded.Add(c);
        if (!string.IsNullOrEmpty(gObj)) custNeeded.Add(gObj);

        // 3) Chọn level cuboid nhỏ nhất đủ cột
        var t = CuboidMap.PickLevel(CuboidMap.TimeCols, timeNeeded);
        var p = CuboidMap.PickLevel(CuboidMap.ProductCols, prodNeeded);
        var c2 = CuboidMap.PickLevel(CuboidMap.CustomerCols, custNeeded);

        var q = new OlapQuery
        {
            Cube = "BH", T = t, P = p, C = c2, Filters = filters,
        };
        var result = _rolap.QueryBanHang(t, p, c2, q);

        var meta = new DashboardMeta
        {
            CubeCode = "BH", CubeName = "BÁN HÀNG",
            ObjectDimCode = "C", ObjectDimName = "Khách hàng",
        };
        foreach (var l in CuboidMap.TimeLevels) meta.TimeLevels[l] = CuboidMap.TimeLabel[l];
        foreach (var l in CuboidMap.ProductLevels) meta.ProductLevels[l] = CuboidMap.ProductLabel[l];
        foreach (var l in CuboidMap.CustomerLevels) meta.ObjectLevels[l] = CuboidMap.CustomerLabel[l];

        var vm = new OlapDashboardVM
        {
            Query = q, Result = result, Meta = meta,
            Options = _rolap.GetFilterOptions(),
            GroupTime = gTime ?? "", GroupObject = gObj ?? "", GroupProduct = gProd ?? "",
        };
        return View(vm);
    }
}
