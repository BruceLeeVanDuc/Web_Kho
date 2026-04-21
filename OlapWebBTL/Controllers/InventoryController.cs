using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Models;
using OlapWebBTL.Services;
using static OlapWebBTL.Services.MdxQueries;

namespace OlapWebBTL.Controllers;

public class InventoryController : Controller
{
    private readonly OlapService _olap;
    public InventoryController(OlapService olap) => _olap = olap;

    public IActionResult Index(string dim = "MatHang", string level = "MoTa")
    {
        var vm = new DashboardVM
        {
            DimensionChon = dim,
            LevelChon = level
        };

        var attr = (dim, level) switch
        {
            ("MatHang", "MoTa") => MatHangMoTa,
            ("MatHang", "KichCo") => MatHangKichCo,
            ("MatHang", "Ma") => MatHangMa,
            ("ThoiGian", "Nam") => ThoiGianNam,
            ("ThoiGian", "Quy") => ThoiGianQuy,
            ("ThoiGian", "Thang") => ThoiGianThang,
            _ => MatHangMoTa
        };

        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongTrongKho}, {MLuuKhoCount} }} ON COLUMNS,
  NON EMPTY {attr}.Members ON ROWS
FROM {Cube}";

        try
        {
            _olap.WithCellSet(mdx, cs =>
            {
                if (cs.Axes.Count < 2) return 0;
                long totalKho = 0; int totalRows = 0;
                var rowAxis = cs.Axes[1];
                for (int i = 0; i < rowAxis.Positions.Count; i++)
                {
                    var mem = rowAxis.Positions[i].Members[0];
                    if (mem.LevelDepth == 0) continue;

                    long kho = SafeLong(cs.Cells[0, i].Value);
                    int cnt = (int)SafeLong(cs.Cells[1, i].Value);

                    vm.Rows.Add(new PivotRow
                    {
                        Label = mem.Caption,
                        SoLuong = kho,
                        SoDon = cnt
                    });
                    vm.BarData.Add(new ChartPoint { Label = mem.Caption, Value = kho });
                    vm.PieData.Add(new ChartPoint { Label = mem.Caption, Value = kho });

                    totalKho += kho;
                    totalRows += cnt;
                }
                vm.TongSoLuong = totalKho;
                vm.TongSoDon = totalRows;
                vm.SoDong = vm.Rows.Count;

                if (totalKho > 0)
                    foreach (var r in vm.Rows)
                        r.TyLe = Math.Round((double)r.SoLuong / totalKho * 100, 2);
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

    private static long SafeLong(object? v)
    {
        if (v == null || v is DBNull) return 0;
        try { return Convert.ToInt64(v); } catch { return 0; }
    }
}
