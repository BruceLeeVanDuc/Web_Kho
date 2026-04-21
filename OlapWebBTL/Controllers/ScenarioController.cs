using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Models;
using OlapWebBTL.Services;

namespace OlapWebBTL.Controllers;

public class ScenarioController : Controller
{
    private readonly RolapService _rolap;
    public ScenarioController(RolapService rolap) => _rolap = rolap;

    // =========================================================
    // 5.3.1 Drill Chart
    // =========================================================
    public IActionResult DrillChart(
        string level = "year",
        int? year = null,
        int? quarter = null,
        string measure = "DoanhThu")
    {
        if (measure != "DoanhThu" && measure != "SL") measure = "DoanhThu";
        if (level != "year" && level != "quarter" && level != "month") level = "year";
        if (level == "quarter" && year == null) level = "year";
        if (level == "month" && year == null) { level = "year"; quarter = null; }

        var vm = new DrillChartVM
        {
            Level = level,
            Year = year,
            Quarter = quarter,
            Measure = measure,
            MeasureLabel = measure == "DoanhThu" ? "Doanh thu" : "Số lượng đặt",
            Years = _rolap.GetFilterOptions().Years,
        };

        string t; var filters = new Dictionary<string, List<string>>();
        if (level == "year")
        {
            t = "T1";
            vm.Title = $"Tổng quan {vm.MeasureLabel} theo Năm";
        }
        else if (level == "quarter")
        {
            t = "T2";
            filters["Nam"] = new List<string> { year!.Value.ToString() };
            vm.Title = $"{vm.MeasureLabel} theo Quý — Năm {year}";
        }
        else // month
        {
            t = "T3";
            filters["Nam"] = new List<string> { year!.Value.ToString() };
            if (quarter != null)
            {
                var months = Enumerable.Range((quarter.Value - 1) * 3 + 1, 3)
                    .Select(m => m.ToString()).ToList();
                filters["Thang"] = months;
                vm.Title = $"{vm.MeasureLabel} theo Tháng — Năm {year} / Quý {quarter}";
            }
            else
            {
                vm.Title = $"{vm.MeasureLabel} theo Tháng — Năm {year}";
            }
        }

        var table = CuboidMap.BanHangTable(t, "P0", "C0");
        var dimCols = CuboidMap.BanHangDimCols(t, "P0", "C0");
        var (sql, pars) = _rolap.BuildCuboidSql(
            table, dimCols, new[] { "SL", "DoanhThu" }, filters, includeDimNames: false);
        vm.TableName = table;
        vm.Sql = sql;

        try
        {
            var rows = _rolap.QueryRaw(sql, pars);
            foreach (var r in rows)
            {
                var yr = ToInt(r.TryGetValue("Nam", out var yv) ? yv : null);
                var qt = r.TryGetValue("Quy", out var qv) ? ToIntNullable(qv) : null;
                var mo = r.TryGetValue("Thang", out var mv) ? ToIntNullable(mv) : null;
                var val = r.TryGetValue(measure, out var mval) ? ToDec(mval) : 0;
                var qty = r.TryGetValue("SL", out var qv2) ? ToLong(qv2) : 0;

                string label = level switch
                {
                    "year" => yr.ToString(),
                    "quarter" => $"Q{qt}",
                    "month" => $"T{mo:D2}",
                    _ => "",
                };

                vm.Points.Add(new DrillPoint
                {
                    Label = label,
                    KeyYear = yr,
                    KeyQuarter = qt,
                    KeyMonth = mo,
                    Value = val,
                    Quantity = qty,
                });
            }
        }
        catch (Exception ex) { vm.Error = ex.Message; }

        ViewData["Title"] = "5.3.1 Drill Chart — Bán hàng";
        return View(vm);
    }

    // =========================================================
    // 5.3.2 Pivot Explorer
    // =========================================================
    public IActionResult Pivot(
        string rows = "Nam",
        string cols = "Loai_khach_hang",
        string measure = "DoanhThu",
        int? year = null,
        string? bang = null)
    {
        var allowed = new[] { "Nam", "Quy", "Thang", "Loai_khach_hang", "Bang", "Ten_thanh_pho" };
        if (!allowed.Contains(rows)) rows = "Nam";
        if (!allowed.Contains(cols)) cols = "Loai_khach_hang";
        if (rows == cols) cols = rows == "Nam" ? "Loai_khach_hang" : "Nam";
        if (measure != "DoanhThu" && measure != "SL") measure = "DoanhThu";

        var vm = new PivotVM
        {
            RowDim = rows,
            ColDim = cols,
            Measure = measure,
            Year = year,
            Bang = bang,
            Options = _rolap.GetFilterOptions(),
        };

        var needTime = new HashSet<string>();
        var needCust = new HashSet<string>();
        foreach (var d in new[] { rows, cols })
        {
            if (d == "Nam" || d == "Quy" || d == "Thang") needTime.Add(d);
            else needCust.Add(d);
        }
        if (year != null) needTime.Add("Nam");
        if (!string.IsNullOrEmpty(bang)) needCust.Add("Bang");
        if (needTime.Contains("Thang")) needTime.Remove("Quy");

        var t = CuboidMap.PickLevel(CuboidMap.TimeCols, needTime);
        var c = CuboidMap.PickLevel(CuboidMap.CustomerCols, needCust);
        var table = CuboidMap.BanHangTable(t, "P0", c);
        vm.TableName = table;

        var dimCols = CuboidMap.BanHangDimCols(t, "P0", c);
        var filters = new Dictionary<string, List<string>>();
        if (year != null) filters["Nam"] = new List<string> { year.Value.ToString() };
        if (!string.IsNullOrEmpty(bang)) filters["Bang"] = new List<string> { bang };

        var (sql, pars) = _rolap.BuildCuboidSql(
            table, dimCols, new[] { "SL", "DoanhThu" }, filters, includeDimNames: false);
        vm.Sql = sql;

        try
        {
            var rowsData = _rolap.QueryRaw(sql, pars);
            var rowSet = new List<string>();
            var colSet = new List<string>();
            foreach (var r in rowsData)
            {
                var rv = r.TryGetValue(rows, out var rr) ? (rr?.ToString() ?? "") : "";
                var cv = r.TryGetValue(cols, out var cc) ? (cc?.ToString() ?? "") : "";
                var mv = r.TryGetValue(measure, out var mm) ? ToDec(mm) : 0;
                if (!rowSet.Contains(rv)) rowSet.Add(rv);
                if (!colSet.Contains(cv)) colSet.Add(cv);
                if (!vm.Cells.ContainsKey(rv)) vm.Cells[rv] = new Dictionary<string, decimal>();
                vm.Cells[rv][cv] = vm.Cells[rv].TryGetValue(cv, out var exist) ? exist + mv : mv;
            }
            rowSet.Sort(StringComparer.OrdinalIgnoreCase);
            colSet.Sort(StringComparer.OrdinalIgnoreCase);
            vm.RowLabels = rowSet;
            vm.ColLabels = colSet;

            foreach (var rv in rowSet)
            {
                decimal rt = 0;
                foreach (var cv in colSet)
                {
                    var v = vm.Cells.TryGetValue(rv, out var dict) && dict.TryGetValue(cv, out var vv) ? vv : 0;
                    rt += v;
                    vm.ColTotals[cv] = vm.ColTotals.TryGetValue(cv, out var ex) ? ex + v : v;
                }
                vm.RowTotals[rv] = rt;
                vm.GrandTotal += rt;
            }
        }
        catch (Exception ex) { vm.Error = ex.Message; }

        ViewData["Title"] = "5.3.2 Pivot Explorer — Bán hàng";
        return View(vm);
    }

    // =========================================================
    // 5.3.3 Drill Across (BH + LK)
    // =========================================================
    public IActionResult DrillAcross(string? product = null, int? year = null)
    {
        var vm = new DrillAcrossVM
        {
            Products = _rolap.GetProductOptions(),
            Years = _rolap.GetFilterOptions().Years,
        };
        if (vm.Years.Count == 0) vm.Years = new List<int> { 2023, 2024 };
        vm.Year = year ?? vm.Years.FirstOrDefault();
        if (vm.Years.Count > 0 && !vm.Years.Contains(vm.Year)) vm.Year = vm.Years.First();

        if (string.IsNullOrEmpty(product) && vm.Products.Count > 0)
            product = vm.Products.First().Ma_MH;
        vm.Product = product ?? "";
        vm.ProductName = vm.Products.FirstOrDefault(p => p.Ma_MH == vm.Product)?.Mo_ta ?? "";

        // chuẩn bị 12 tháng
        for (int m = 1; m <= 12; m++)
            vm.Months.Add(new MonthPoint { Month = m });

        if (string.IsNullOrEmpty(vm.Product))
        {
            ViewData["Title"] = "5.3.3 Drill Across — Sales × Inventory";
            return View(vm);
        }

        var filters = new Dictionary<string, List<string>>
        {
            ["Nam"] = new() { vm.Year.ToString() },
            ["Ma_MH"] = new() { vm.Product },
        };

        // Sales — Cuboid_BH_T3_P1_C0
        var bhTable = CuboidMap.BanHangTable("T3", "P1", "C0");
        var bhDims = CuboidMap.BanHangDimCols("T3", "P1", "C0");
        var (bhSql, bhPars) = _rolap.BuildCuboidSql(bhTable, bhDims,
            new[] { "SL", "DoanhThu" }, filters, includeDimNames: false);
        vm.SalesTable = bhTable;

        // Inventory — Cuboid_LK_T3_P1_S0
        var lkTable = CuboidMap.LuuKhoTable("T3", "P1", "S0");
        var lkDims = CuboidMap.LuuKhoDimCols("T3", "P1", "S0");
        var (lkSql, lkPars) = _rolap.BuildCuboidSql(lkTable, lkDims,
            new[] { "TongTon" }, filters, includeDimNames: false);
        vm.InventoryTable = lkTable;

        try
        {
            foreach (var r in _rolap.QueryRaw(bhSql, bhPars))
            {
                var m = r.TryGetValue("Thang", out var mv) ? ToInt(mv) : 0;
                if (m < 1 || m > 12) continue;
                vm.Months[m - 1].Quantity = r.TryGetValue("SL", out var sl) ? ToLong(sl) : 0;
                vm.Months[m - 1].Sales = r.TryGetValue("DoanhThu", out var dt) ? ToDec(dt) : 0;
            }
            foreach (var r in _rolap.QueryRaw(lkSql, lkPars))
            {
                var m = r.TryGetValue("Thang", out var mv) ? ToInt(mv) : 0;
                if (m < 1 || m > 12) continue;
                vm.Months[m - 1].Inventory = r.TryGetValue("TongTon", out var tt) ? ToDec(tt) : 0;
            }
            // coverage[T] = Inv[T] / Qty[T+1]
            for (int i = 0; i < 11; i++)
            {
                var inv = vm.Months[i].Inventory;
                var nextQty = vm.Months[i + 1].Quantity;
                vm.Months[i].Coverage = nextQty == 0 ? null : Math.Round((double)inv / nextQty, 2);
            }
            vm.TotalSales = vm.Months.Sum(x => x.Sales);
            vm.TotalQty = vm.Months.Sum(x => x.Quantity);
            var invList = vm.Months.Where(x => x.Inventory > 0).ToList();
            vm.AvgInventory = invList.Count == 0 ? 0 :
                Math.Round(invList.Average(x => x.Inventory), 0);
        }
        catch (Exception ex) { vm.Error = ex.Message; }

        ViewData["Title"] = "5.3.3 Drill Across — Sales × Inventory";
        return View(vm);
    }

    // ---------- helpers ----------
    private static decimal ToDec(object? v)
    {
        if (v == null || v is DBNull) return 0;
        try { return Convert.ToDecimal(v); } catch { return 0; }
    }
    private static long ToLong(object? v)
    {
        if (v == null || v is DBNull) return 0;
        try { return Convert.ToInt64(v); } catch { return 0; }
    }
    private static int ToInt(object? v)
    {
        if (v == null || v is DBNull) return 0;
        try { return Convert.ToInt32(v); } catch { return 0; }
    }
    private static int? ToIntNullable(object? v)
    {
        if (v == null || v is DBNull) return null;
        try { return Convert.ToInt32(v); } catch { return null; }
    }
}
