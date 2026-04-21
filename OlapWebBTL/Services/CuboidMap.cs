namespace OlapWebBTL.Services;

/// <summary>
/// Ánh xạ (level T, P, C/S) → tên bảng cuboid vật lý + danh sách cột dim trong bảng.
/// </summary>
public static class CuboidMap
{
    public static readonly string[] TimeLevels = { "T0", "T1", "T2", "T3", "T4" };
    public static readonly string[] ProductLevels = { "P0", "P1" };
    public static readonly string[] CustomerLevels = { "C0", "C1", "C2", "C3", "C4", "C5", "C6" };
    public static readonly string[] StoreLevels = { "S0", "S1", "S2", "S3" };

    public static readonly Dictionary<string, string> TimeLabel = new()
    {
        ["T0"] = "Tổng", ["T1"] = "Năm", ["T2"] = "Năm+Quý",
        ["T3"] = "Năm+Tháng", ["T4"] = "Năm+Tháng+Ngày",
    };
    public static readonly Dictionary<string, string> ProductLabel = new()
    { ["P0"] = "Tổng", ["P1"] = "Mã MH" };
    public static readonly Dictionary<string, string> CustomerLabel = new()
    {
        ["C0"] = "Tổng", ["C1"] = "Loại KH", ["C2"] = "Bang",
        ["C3"] = "Bang+TP", ["C4"] = "Bang+TP+Mã KH",
        ["C5"] = "Bang+Loại KH", ["C6"] = "Bang+TP+Loại KH",
    };
    public static readonly Dictionary<string, string> StoreLabel = new()
    {
        ["S0"] = "Tổng", ["S1"] = "Bang", ["S2"] = "Bang+TP", ["S3"] = "Bang+TP+Mã CH",
    };

    public static readonly Dictionary<string, string[]> TimeCols = new()
    {
        ["T0"] = Array.Empty<string>(),
        ["T1"] = new[] { "Nam" },
        ["T2"] = new[] { "Nam", "Quy" },
        ["T3"] = new[] { "Nam", "Thang" },
        ["T4"] = new[] { "Nam", "Thang", "Ngay" },
    };
    public static readonly Dictionary<string, string[]> ProductCols = new()
    {
        ["P0"] = Array.Empty<string>(),
        ["P1"] = new[] { "Ma_MH" },
    };
    public static readonly Dictionary<string, string[]> CustomerCols = new()
    {
        ["C0"] = Array.Empty<string>(),
        ["C1"] = new[] { "Loai_khach_hang" },
        ["C2"] = new[] { "Bang" },
        ["C3"] = new[] { "Bang", "Ten_thanh_pho" },
        ["C4"] = new[] { "Bang", "Ten_thanh_pho", "Ma_KH" },
        ["C5"] = new[] { "Bang", "Loai_khach_hang" },
        ["C6"] = new[] { "Bang", "Ten_thanh_pho", "Loai_khach_hang" },
    };
    public static readonly Dictionary<string, string[]> StoreCols = new()
    {
        ["S0"] = Array.Empty<string>(),
        ["S1"] = new[] { "Bang" },
        ["S2"] = new[] { "Bang", "Ten_thanh_pho" },
        ["S3"] = new[] { "Bang", "Ten_thanh_pho", "Ma_CH" },
    };

    public static string BanHangTable(string t, string p, string c) => $"Cuboid_BH_{t}_{p}_{c}";
    public static string LuuKhoTable(string t, string p, string s) => $"Cuboid_LK_{t}_{p}_{s}";

    public static List<string> BanHangDimCols(string t, string p, string c)
    {
        var cols = new List<string>();
        cols.AddRange(TimeCols[t]);
        cols.AddRange(ProductCols[p]);
        cols.AddRange(CustomerCols[c]);
        return cols;
    }

    public static List<string> LuuKhoDimCols(string t, string p, string s)
    {
        var cols = new List<string>();
        cols.AddRange(TimeCols[t]);
        cols.AddRange(ProductCols[p]);
        cols.AddRange(StoreCols[s]);
        return cols;
    }

    /// <summary>
    /// Tự chọn level nhỏ nhất mà cột có sẵn ⊇ cột cần thiết (cho filter + group-by).
    /// Nếu không có level nào đủ, trả về level overlap nhiều nhất.
    /// </summary>
    public static string PickLevel(Dictionary<string, string[]> levels, ISet<string> needed)
    {
        if (needed.Count == 0) return levels.First(l => l.Value.Length == 0).Key;
        foreach (var kv in levels.OrderBy(k => k.Value.Length))
        {
            if (needed.IsSubsetOf(kv.Value)) return kv.Key;
        }
        return levels
            .OrderByDescending(kv => needed.Intersect(kv.Value).Count())
            .ThenBy(kv => kv.Value.Length)
            .First().Key;
    }
}
