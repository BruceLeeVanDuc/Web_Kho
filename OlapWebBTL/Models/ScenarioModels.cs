namespace OlapWebBTL.Models;

/// <summary>
/// 5.3.1 Drill Chart — bar chart theo thời gian, click để khoan sâu.
/// </summary>
public class DrillChartVM
{
    public string Level { get; set; } = "year";   // year | quarter | month
    public int? Year { get; set; }
    public int? Quarter { get; set; }
    public string Measure { get; set; } = "DoanhThu"; // DoanhThu | SL
    public string TableName { get; set; } = "";
    public string Sql { get; set; } = "";
    public string? Error { get; set; }
    public string Title { get; set; } = "";
    public string MeasureLabel { get; set; } = "";
    public List<DrillPoint> Points { get; set; } = new();
    public List<int> Years { get; set; } = new();
}

public class DrillPoint
{
    public string Label { get; set; } = "";   // hiển thị
    public int KeyYear { get; set; }
    public int? KeyQuarter { get; set; }
    public int? KeyMonth { get; set; }
    public decimal Value { get; set; }
    public long Quantity { get; set; }
}

/// <summary>
/// 5.3.2 Pivot Explorer — ma trận Rows × Cols.
/// </summary>
public class PivotVM
{
    public string RowDim { get; set; } = "Nam";
    public string ColDim { get; set; } = "Loai_khach_hang";
    public string Measure { get; set; } = "DoanhThu"; // DoanhThu | SL
    public string TableName { get; set; } = "";
    public string Sql { get; set; } = "";
    public string? Error { get; set; }

    public int? Year { get; set; }
    public string? Bang { get; set; }

    public List<string> RowLabels { get; set; } = new();
    public List<string> ColLabels { get; set; } = new();
    public Dictionary<string, Dictionary<string, decimal>> Cells { get; set; } = new();
    public Dictionary<string, decimal> RowTotals { get; set; } = new();
    public Dictionary<string, decimal> ColTotals { get; set; } = new();
    public decimal GrandTotal { get; set; }

    public FilterOptions Options { get; set; } = new();

    public static readonly Dictionary<string, string> DimLabels = new()
    {
        ["Nam"] = "Năm",
        ["Quy"] = "Quý",
        ["Thang"] = "Tháng",
        ["Loai_khach_hang"] = "Loại KH",
        ["Bang"] = "Bang",
        ["Ten_thanh_pho"] = "Thành phố",
    };
}

/// <summary>
/// 5.3.3 Drill Across — 1 sản phẩm × 12 tháng, Sales (BH) + Inventory (LK).
/// </summary>
public class DrillAcrossVM
{
    public string Product { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Year { get; set; }
    public string SalesTable { get; set; } = "";
    public string InventoryTable { get; set; } = "";
    public string? Error { get; set; }

    public List<MonthPoint> Months { get; set; } = new();
    public List<ProductOpt> Products { get; set; } = new();
    public List<int> Years { get; set; } = new();

    public decimal TotalSales { get; set; }
    public long TotalQty { get; set; }
    public decimal AvgInventory { get; set; }
}

public class MonthPoint
{
    public int Month { get; set; }
    public long Quantity { get; set; }
    public decimal Sales { get; set; }
    public decimal Inventory { get; set; }
    public double? Coverage { get; set; }   // Inv[T] / Qty[T+1]
}

public class ProductOpt
{
    public string Ma_MH { get; set; } = "";
    public string Mo_ta { get; set; } = "";
    public string Display => string.IsNullOrEmpty(Mo_ta) ? Ma_MH : $"{Ma_MH} — {Mo_ta}";
}
