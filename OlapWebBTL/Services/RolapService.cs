using System.Data;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using OlapWebBTL.Models;

namespace OlapWebBTL.Services;

/// <summary>
/// Truy vấn cuboid vật lý trong DW_QuanLyPhanTich (SQL Server).
/// </summary>
public class RolapService
{
    private readonly string _connStr;

    public RolapService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException(
                "Missing ConnectionStrings:DataWarehouse in appsettings.json");
    }

    private IDbConnection Open()
    {
        var c = new SqlConnection(_connStr);
        c.Open();
        return c;
    }

    public string TestConnection()
    {
        using var c = Open();
        var db = c.ExecuteScalar<string>("SELECT DB_NAME()");
        var ver = c.ExecuteScalar<string>("SELECT @@VERSION");
        return $"OK - DB: {db}\n{ver}";
    }

    // ----------- Filter options (cho dropdown sidebar) -----------

    public FilterOptions GetFilterOptions()
    {
        var opt = new FilterOptions();
        try
        {
            using var c = Open();
            opt.Years = c.Query<int>("SELECT DISTINCT Nam FROM Dim_ThoiGian WHERE Nam IS NOT NULL ORDER BY Nam DESC").ToList();
            opt.Bangs = c.Query<string>("SELECT DISTINCT Bang FROM Dim_DiaChi WHERE Bang IS NOT NULL ORDER BY Bang").ToList();
            opt.Cities = c.Query<string>("SELECT DISTINCT Ten_thanh_pho FROM Dim_DiaChi WHERE Ten_thanh_pho IS NOT NULL ORDER BY Ten_thanh_pho").ToList();
            opt.CustomerTypes = c.Query<string>("SELECT DISTINCT Loai_khach_hang FROM Dim_KhachHang WHERE Loai_khach_hang IS NOT NULL ORDER BY Loai_khach_hang").ToList();

            var pairs = c.Query<(string Bang, string Tp)>(
                "SELECT Bang, Ten_thanh_pho AS Tp FROM Dim_DiaChi WHERE Bang IS NOT NULL AND Ten_thanh_pho IS NOT NULL").ToList();
            foreach (var (bang, tp) in pairs)
            {
                if (!opt.CitiesByBang.ContainsKey(bang)) opt.CitiesByBang[bang] = new List<string>();
                if (!opt.CitiesByBang[bang].Contains(tp)) opt.CitiesByBang[bang].Add(tp);
            }
            foreach (var k in opt.CitiesByBang.Keys.ToList()) opt.CitiesByBang[k].Sort();
        }
        catch
        {
            // nuốt lỗi — dropdown sẽ rỗng nhưng trang vẫn load
        }
        return opt;
    }

    // ----------- Build SQL động -----------

    public (string Sql, DynamicParameters Params) BuildCuboidSql(
        string tableName,
        IReadOnlyList<string> dimCols,
        IReadOnlyList<string> measureCols,
        Dictionary<string, List<string>> filters,
        bool includeDimNames = true)
    {
        var p = new DynamicParameters();
        var sb = new StringBuilder();

        var selectList = new List<string>();
        var groupList = new List<string>();
        var joins = new List<string>();
        var displayCols = new List<string>();

        if (includeDimNames)
        {
            if (dimCols.Contains("Ma_MH"))
            {
                joins.Add("LEFT JOIN Dim_MatHang mh ON mh.Ma_MH = c.Ma_MH");
                displayCols.Add("mh.Mo_ta AS Mo_ta");
                displayCols.Add("mh.Kich_co AS Kich_co");
                displayCols.Add("mh.Trong_luong AS Trong_luong");
                displayCols.Add("mh.Gia AS Don_gia");
            }
            if (dimCols.Contains("Ma_KH"))
            {
                joins.Add("LEFT JOIN Dim_KhachHang kh ON kh.Ma_KH = c.Ma_KH");
                displayCols.Add("kh.Ten_khach_hang AS Ten_KH");
            }
            if (dimCols.Contains("Ma_CH"))
            {
                joins.Add("LEFT JOIN Dim_CuaHang ch ON ch.Ma_CH = c.Ma_CH");
                displayCols.Add("ch.So_dien_thoai AS SDT_CH");
            }
        }

        foreach (var col in dimCols)
        {
            selectList.Add($"c.[{col}]");
            groupList.Add($"c.[{col}]");
        }
        foreach (var dc in displayCols)
        {
            selectList.Add(dc);
            var raw = dc.Split(" AS ")[0];
            groupList.Add(raw);
        }
        foreach (var m in measureCols)
        {
            selectList.Add($"SUM(c.[{m}]) AS [{m}]");
        }

        sb.Append("SELECT ").Append(string.Join(", ", selectList));
        sb.Append(" FROM [").Append(tableName).Append("] c");
        foreach (var j in joins) sb.Append(' ').Append(j);

        var whereList = new List<string>();
        int pi = 0;
        foreach (var kv in filters)
        {
            if (kv.Value == null || kv.Value.Count == 0) continue;
            // chỉ filter cột có trong cuboid
            if (!dimCols.Contains(kv.Key)) continue;
            var names = new List<string>();
            foreach (var v in kv.Value)
            {
                var pName = $"p{pi++}";
                names.Add("@" + pName);
                p.Add(pName, v);
            }
            whereList.Add($"c.[{kv.Key}] IN ({string.Join(",", names)})");
        }
        if (whereList.Count > 0)
            sb.Append(" WHERE ").Append(string.Join(" AND ", whereList));

        if (groupList.Count > 0)
            sb.Append(" GROUP BY ").Append(string.Join(", ", groupList));

        if (dimCols.Count > 0)
            sb.Append(" ORDER BY ").Append(string.Join(", ", dimCols.Select(d => $"c.[{d}]")));

        return (sb.ToString(), p);
    }

    public OlapResult QueryBanHang(string t, string p, string c, OlapQuery q)
    {
        var table = CuboidMap.BanHangTable(t, p, c);
        var dimCols = CuboidMap.BanHangDimCols(t, p, c);
        var measures = new[] { "SL", "DoanhThu" };
        return QueryGeneric(table, dimCols, measures, q, isBanHang: true);
    }

    public OlapResult QueryLuuKho(string t, string p, string s, OlapQuery q)
    {
        var table = CuboidMap.LuuKhoTable(t, p, s);
        var dimCols = CuboidMap.LuuKhoDimCols(t, p, s);
        var measures = new[] { "TongTon" };
        return QueryGeneric(table, dimCols, measures, q, isBanHang: false);
    }

    private OlapResult QueryGeneric(
        string table,
        List<string> dimCols,
        string[] measures,
        OlapQuery q,
        bool isBanHang)
    {
        var result = new OlapResult { TableName = table };
        var (sql, sqlParams) = BuildCuboidSql(table, dimCols, measures, q.Filters, includeDimNames: true);
        result.Sql = sql;

        try
        {
            using var conn = Open();
            var rows = conn.Query(sql, sqlParams).ToList();

            result.DimColumns.AddRange(dimCols);
            if (dimCols.Contains("Ma_MH"))
            {
                result.DimColumns.Add("Mo_ta");
                result.DimColumns.Add("Kich_co");
                result.DimColumns.Add("Trong_luong");
                result.DimColumns.Add("Don_gia");
            }
            if (dimCols.Contains("Ma_KH")) result.DimColumns.Add("Ten_KH");
            if (dimCols.Contains("Ma_CH")) result.DimColumns.Add("SDT_CH");

            if (isBanHang)
                result.MeasureColumns.AddRange(new[] { "SL", "DoanhThu", "Giá TB" });
            else
                result.MeasureColumns.Add("TongTon");

            foreach (IDictionary<string, object> r in rows)
            {
                var row = new OlapRow();
                foreach (var d in result.DimColumns)
                    row.DimValues.Add(r.TryGetValue(d, out var v) ? (v?.ToString() ?? "") : "");

                if (isBanHang)
                {
                    row.Measure1 = r.TryGetValue("DoanhThu", out var dt) ? ToDec(dt) : 0;
                    row.Measure2 = r.TryGetValue("SL", out var sl) ? ToLong(sl) : 0;
                    row.Avg = row.Measure2 == 0 ? 0 : Math.Round(row.Measure1 / row.Measure2, 2);
                }
                else
                {
                    row.Measure1 = r.TryGetValue("TongTon", out var tt) ? ToDec(tt) : 0;
                }
                result.Rows.Add(row);
                result.TotalMeasure1 += row.Measure1;
                result.TotalMeasure2 += row.Measure2;
            }
            result.TotalRows = result.Rows.Count;

            if (result.TotalMeasure1 > 0)
            {
                foreach (var r in result.Rows)
                    r.TyLe = Math.Round((double)(r.Measure1 / result.TotalMeasure1) * 100, 2);
            }

            var chartRows = result.Rows
                .OrderByDescending(r => r.Measure1)
                .Take(15)
                .ToList();
            foreach (var r in chartRows)
            {
                var label = string.Join(" | ", r.DimValues.Where(v => !string.IsNullOrEmpty(v)).Take(3));
                if (string.IsNullOrEmpty(label)) label = "(Tổng)";
                result.BarData.Add(new ChartPoint { Label = label, Value = (double)r.Measure1 });
                result.PieData.Add(new ChartPoint { Label = label, Value = (double)r.Measure1 });
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    public List<string> GetDistinctValues(string tableName, string column)
    {
        try
        {
            using var conn = Open();
            var sql = $"SELECT DISTINCT [{column}] FROM [{tableName}] WHERE [{column}] IS NOT NULL ORDER BY 1";
            return conn.Query<object>(sql).Select(v => v?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
        catch { return new List<string>(); }
    }

    /// <summary>
    /// Chạy 1 câu SQL + tham số, trả về list dict (cột → giá trị).
    /// </summary>
    public List<IDictionary<string, object>> QueryRaw(string sql, DynamicParameters pars)
    {
        using var conn = Open();
        return conn.Query(sql, pars).Cast<IDictionary<string, object>>().ToList();
    }

    /// <summary>
    /// Danh sách mã mặt hàng + mô tả cho dropdown sản phẩm.
    /// </summary>
    public List<ProductOpt> GetProductOptions()
    {
        try
        {
            using var c = Open();
            return c.Query<ProductOpt>(
                "SELECT Ma_MH, ISNULL(Mo_ta,'') AS Mo_ta FROM Dim_MatHang ORDER BY Ma_MH")
                .ToList();
        }
        catch { return new List<ProductOpt>(); }
    }

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
}
