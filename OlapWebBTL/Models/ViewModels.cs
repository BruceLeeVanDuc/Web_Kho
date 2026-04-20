namespace OlapWebBTL.Models;

public class DashboardVM
{
    public decimal TongDoanhThu { get; set; }
    public long TongSoLuong { get; set; }
    public int TongSoDon { get; set; }
    public int SoDong { get; set; }

    public List<ChartPoint> BarData { get; set; } = new();
    public List<ChartPoint> PieData { get; set; } = new();
    public List<PivotRow> Rows { get; set; } = new();

    public string DimensionChon { get; set; } = "ThoiGian";
    public string LevelChon { get; set; } = "Nam";
    public string? FilterLabel { get; set; }
}

public class ChartPoint
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
}

public class PivotRow
{
    public string Label { get; set; } = "";
    public decimal DoanhThu { get; set; }
    public long SoLuong { get; set; }
    public int SoDon { get; set; }
    public double TyLe { get; set; }
}

public class ReportResult
{
    public string Title { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public string? Mdx { get; set; }
    public string? Error { get; set; }
}

public class DiagInfo
{
    public string? ConnectionStatus { get; set; }
    public List<string> Cubes { get; set; } = new();
    public List<string> Dimensions { get; set; } = new();
    public List<string> Measures { get; set; } = new();
    public List<string> Hierarchies { get; set; } = new();
    public string? Error { get; set; }
}
