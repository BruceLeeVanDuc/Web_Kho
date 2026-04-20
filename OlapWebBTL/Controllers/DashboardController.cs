using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Models;
using OlapWebBTL.Services;

namespace OlapWebBTL.Controllers;

public class DashboardController : Controller
{
    private readonly OlapService _olap;
    public DashboardController(OlapService olap) => _olap = olap;

    public IActionResult Index(string dim = "ThoiGian", string level = "Nam", string? filter = null)
    {
        var vm = new DashboardVM
        {
            DimensionChon = dim,
            LevelChon = level,
            FilterLabel = filter
        };

        var attrUnique = MapAttr(dim, level);
        var whereClause = BuildWhere(dim, filter);

        var mdx = $@"
SELECT
  NON EMPTY {{
    {MdxQueries.MGiaDat},
    {MdxQueries.MSoLuongDat},
    {MdxQueries.MDatCount}
  }} ON COLUMNS,
  NON EMPTY {attrUnique}.Members ON ROWS
FROM {MdxQueries.Cube}
{whereClause}";

        try
        {
            _olap.WithCellSet(mdx, cs =>
            {
                if (cs.Axes.Count < 2) return 0;
                decimal totalRev = 0; long totalQty = 0; int totalRows = 0;
                var rowAxis = cs.Axes[1];
                for (int i = 0; i < rowAxis.Positions.Count; i++)
                {
                    var mem = rowAxis.Positions[i].Members[0];
                    if (mem.LevelDepth == 0) continue;
                    var label = mem.Caption;

                    decimal rev = SafeDec(cs.Cells[0, i].Value);
                    long qty = SafeLong(cs.Cells[1, i].Value);
                    int cnt = (int)SafeLong(cs.Cells[2, i].Value);

                    vm.Rows.Add(new PivotRow
                    {
                        Label = label,
                        DoanhThu = rev,
                        SoLuong = qty,
                        SoDon = cnt
                    });
                    vm.BarData.Add(new ChartPoint { Label = label, Value = (double)rev });
                    vm.PieData.Add(new ChartPoint { Label = label, Value = (double)rev });

                    totalRev += rev;
                    totalQty += qty;
                    totalRows += cnt;
                }
                vm.TongDoanhThu = totalRev;
                vm.TongSoLuong = totalQty;
                vm.TongSoDon = totalRows;
                vm.SoDong = vm.Rows.Count;

                if (totalRev > 0)
                {
                    foreach (var r in vm.Rows)
                        r.TyLe = Math.Round((double)(r.DoanhThu / totalRev) * 100, 2);
                }
                return 0;
            });
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Mdx = mdx;
        }

        return View(vm);
    }

    [HttpGet]
    public IActionResult GetFilters(string dim)
    {
        var attr = dim switch
        {
            "ThoiGian" => MdxQueries.ThoiGianNam,
            "MatHang" => MdxQueries.MatHangMoTa,
            "KhachHang" => MdxQueries.KhachHangLoai,
            "CuaHang" => MdxQueries.CuaHangBang,
            _ => MdxQueries.ThoiGianNam
        };
        var list = _olap.GetMembers(attr);
        return Json(list);
    }

    private static string MapAttr(string dim, string level) => (dim, level) switch
    {
        ("ThoiGian", "Nam") => MdxQueries.ThoiGianNam,
        ("ThoiGian", "Quy") => MdxQueries.ThoiGianQuy,
        ("ThoiGian", "Thang") => MdxQueries.ThoiGianThang,
        ("ThoiGian", "Ngay") => MdxQueries.ThoiGianNgay,
        ("MatHang", _) => MdxQueries.MatHangMoTa,
        ("KhachHang", "Loai") => MdxQueries.KhachHangLoai,
        ("KhachHang", "TenKH") => MdxQueries.KhachHangTen,
        ("KhachHang", "ThanhPho") => MdxQueries.KhachHangThanhPho,
        ("CuaHang", "Bang") => MdxQueries.CuaHangBang,
        ("CuaHang", "ThanhPho") => MdxQueries.CuaHangThanhPho,
        ("CuaHang", "MaCH") => MdxQueries.CuaHangMa,
        _ => MdxQueries.ThoiGianNam
    };

    private static string BuildWhere(string dim, string? filter)
    {
        if (string.IsNullOrEmpty(filter)) return "";
        return dim switch
        {
            "ThoiGian" => $"WHERE ({MdxQueries.ThoiGianNam}.&[{filter}])",
            "MatHang" => $"WHERE ({MdxQueries.MatHangMoTa}.&[{filter}])",
            "KhachHang" => $"WHERE ({MdxQueries.KhachHangLoai}.&[{filter}])",
            "CuaHang" => $"WHERE ({MdxQueries.CuaHangBang}.&[{filter}])",
            _ => ""
        };
    }

    private static decimal SafeDec(object? v)
    {
        if (v == null || v is DBNull) return 0;
        try { return Convert.ToDecimal(v); } catch { return 0; }
    }
    private static long SafeLong(object? v)
    {
        if (v == null || v is DBNull) return 0;
        try { return Convert.ToInt64(v); } catch { return 0; }
    }
}
