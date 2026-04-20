using Microsoft.AnalysisServices.AdomdClient;
using OlapWebBTL.Models;
using System.Data;

namespace OlapWebBTL.Services;

public class OlapService
{
    private readonly string _connectionString;
    public string CubeName { get; }

    public OlapService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("OlapCube")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:OlapCube");
        CubeName = config["OlapCube:CubeName"] ?? "DW Quan Ly Phan Tich";
    }

    public string TestConnection()
    {
        using var conn = new AdomdConnection(_connectionString);
        conn.Open();
        return $"OK - DB: {conn.Database}, Version: {conn.ServerVersion}";
    }

    public DiagInfo GetDiagnostics()
    {
        var info = new DiagInfo();
        try
        {
            using var conn = new AdomdConnection(_connectionString);
            conn.Open();
            info.ConnectionStatus = $"OK - Database: {conn.Database}, Server version: {conn.ServerVersion}";

            var cubes = conn.GetSchemaDataSet("MDSCHEMA_CUBES", null).Tables[0];
            foreach (DataRow row in cubes.Rows)
            {
                var name = row["CUBE_NAME"]?.ToString() ?? "";
                if (!name.StartsWith("$")) info.Cubes.Add(name);
            }

            var measures = conn.GetSchemaDataSet("MDSCHEMA_MEASURES", null).Tables[0];
            foreach (DataRow row in measures.Rows)
            {
                var cube = row["CUBE_NAME"]?.ToString() ?? "";
                var name = row["MEASURE_NAME"]?.ToString() ?? "";
                var unique = row["MEASURE_UNIQUE_NAME"]?.ToString() ?? "";
                if (cube == CubeName) info.Measures.Add($"{name}  →  {unique}");
            }

            var dims = conn.GetSchemaDataSet("MDSCHEMA_DIMENSIONS", null).Tables[0];
            foreach (DataRow row in dims.Rows)
            {
                var cube = row["CUBE_NAME"]?.ToString() ?? "";
                var name = row["DIMENSION_NAME"]?.ToString() ?? "";
                var unique = row["DIMENSION_UNIQUE_NAME"]?.ToString() ?? "";
                if (cube == CubeName && !name.StartsWith("Measures"))
                    info.Dimensions.Add($"{name}  →  {unique}");
            }

            var hiers = conn.GetSchemaDataSet("MDSCHEMA_HIERARCHIES", null).Tables[0];
            foreach (DataRow row in hiers.Rows)
            {
                var cube = row["CUBE_NAME"]?.ToString() ?? "";
                var dimName = row["DIMENSION_UNIQUE_NAME"]?.ToString() ?? "";
                var hName = row["HIERARCHY_NAME"]?.ToString() ?? "";
                var unique = row["HIERARCHY_UNIQUE_NAME"]?.ToString() ?? "";
                if (cube == CubeName && !dimName.Contains("Measures"))
                    info.Hierarchies.Add($"{dimName}.{hName}  →  {unique}");
            }
        }
        catch (Exception ex)
        {
            info.Error = ex.Message;
        }
        return info;
    }

    /// <summary>
    /// Chạy MDX, trả về CellSet (caller tự quản connection lifecycle qua callback).
    /// </summary>
    public T WithCellSet<T>(string mdx, Func<CellSet, T> handler)
    {
        using var conn = new AdomdConnection(_connectionString);
        conn.Open();
        using var cmd = new AdomdCommand(mdx, conn);
        var cs = cmd.ExecuteCellSet();
        return handler(cs);
    }

    public List<string> GetMembers(string attributeUniqueName)
    {
        var mdx = $"SELECT {{}} ON 0, {attributeUniqueName}.Members ON 1 FROM {MdxQueries.Cube}";
        var result = new List<string>();
        try
        {
            WithCellSet(mdx, cs =>
            {
                if (cs.Axes.Count < 2) return 0;
                foreach (Position pos in cs.Axes[1].Positions)
                {
                    var m = pos.Members[0];
                    if (m.LevelDepth == 0) continue;
                    result.Add(m.Caption);
                }
                return 0;
            });
        }
        catch { }
        return result;
    }

    public ReportResult ExecuteAsTable(string mdx, string title)
    {
        var r = new ReportResult { Title = title, Mdx = mdx };
        try
        {
            WithCellSet(mdx, cs =>
            {
                if (cs.Axes.Count == 0)
                {
                    r.Error = "Query không trả về trục nào.";
                    return 0;
                }

                var colAxis = cs.Axes[0];
                Axis? rowAxis = cs.Axes.Count > 1 ? cs.Axes[1] : null;

                // Headers for row-axis dimensions
                if (rowAxis != null && rowAxis.Positions.Count > 0)
                {
                    foreach (Member m in rowAxis.Positions[0].Members)
                        r.Columns.Add(m.ParentLevel.Caption);
                }
                // Headers for measure columns
                foreach (Position pos in colAxis.Positions)
                {
                    var names = new List<string>();
                    foreach (Member m in pos.Members) names.Add(m.Caption);
                    r.Columns.Add(string.Join(" / ", names));
                }

                int rowCount = rowAxis?.Positions.Count ?? 1;
                int colCount = colAxis.Positions.Count;
                for (int i = 0; i < rowCount; i++)
                {
                    var row = new List<string>();
                    if (rowAxis != null)
                    {
                        foreach (Member m in rowAxis.Positions[i].Members)
                            row.Add(m.Caption);
                    }
                    for (int j = 0; j < colCount; j++)
                    {
                        var cell = cs.Cells[j, i];
                        row.Add(cell.FormattedValue ?? "");
                    }
                    r.Rows.Add(row);
                }
                return 0;
            });
        }
        catch (Exception ex)
        {
            r.Error = ex.Message;
        }
        return r;
    }
}
