namespace OlapWebBTL.Models;

/// <summary>
/// Request từ UI: cube loại gì, ở level nào, filter gì, có pivot không.
/// </summary>
public class OlapQuery
{
    public string Cube { get; set; } = "BH";   // "BH" | "LK"
    public string T { get; set; } = "T1";
    public string P { get; set; } = "P0";
    public string C { get; set; } = "C0";      // chỉ dùng khi Cube=BH
    public string S { get; set; } = "S0";      // chỉ dùng khi Cube=LK

    /// <summary>Filter slice/dice: tên cột → list giá trị cần lọc (OR).</summary>
    public Dictionary<string, List<string>> Filters { get; set; } = new();

    /// <summary>Cột nào đem lên làm row của pivot. Các cột còn lại hiện nguyên.</summary>
    public string? PivotColumn { get; set; }

    /// <summary>Chỉ số measure dùng để render bảng pivot/chart.</summary>
    public string MeasureForChart { get; set; } = "DoanhThu"; // BH: DoanhThu|SL ; LK: TongTon
}

/// <summary>
/// Kết quả OLAP sau khi query khối + format ra bảng.
/// </summary>
public class OlapResult
{
    public string TableName { get; set; } = "";
    public string Sql { get; set; } = "";
    public string? Error { get; set; }

    public List<string> DimColumns { get; set; } = new();    // tên cột dim (có tên hiển thị)
    public List<string> MeasureColumns { get; set; } = new(); // tên measure
    public List<OlapRow> Rows { get; set; } = new();

    // Tổng hợp toàn khối (trước filter) cho KPI box
    public decimal TotalMeasure1 { get; set; }  // BH: DoanhThu ; LK: TongTon
    public long TotalMeasure2 { get; set; }     // BH: SL       ; LK: (SoLuongTrongKho ~ long)
    public int TotalRows { get; set; }

    // Dữ liệu vẽ chart
    public List<ChartPoint> BarData { get; set; } = new();
    public List<ChartPoint> PieData { get; set; } = new();
}

public class OlapRow
{
    public List<string> DimValues { get; set; } = new();
    public decimal Measure1 { get; set; }   // DoanhThu (BH) | TongTon (LK)
    public long Measure2 { get; set; }      // SL (BH)       | 0 (LK)
    public decimal Avg { get; set; }        // DoanhThu / SL (chỉ BH)
    public double TyLe { get; set; }        // % của Measure1 so với tổng
}

/// <summary>
/// Metadata trả cho UI để build dropdown.
/// </summary>
public class DashboardMeta
{
    public string CubeCode { get; set; } = "BH";
    public string CubeName { get; set; } = "Bán Hàng";

    public Dictionary<string, string> TimeLevels { get; set; } = new();
    public Dictionary<string, string> ProductLevels { get; set; } = new();
    public Dictionary<string, string> ObjectLevels { get; set; } = new(); // C... cho BH hoặc S... cho LK

    public string ObjectDimCode { get; set; } = "C"; // "C" | "S"
    public string ObjectDimName { get; set; } = "Khách hàng";

    /// <summary>Danh sách giá trị cho từng cột filter (lấy từ dim table).</summary>
    public Dictionary<string, List<string>> FilterValues { get; set; } = new();
}
