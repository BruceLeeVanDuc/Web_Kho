namespace OlapWebBTL.Models;

public class OlapDashboardVM
{
    public OlapQuery Query { get; set; } = new();
    public OlapResult Result { get; set; } = new();
    public DashboardMeta Meta { get; set; } = new();
    public FilterOptions Options { get; set; } = new();

    /// <summary>Group-by đang chọn cho từng dim (tên cột; rỗng = không group).</summary>
    public string GroupTime { get; set; } = "";
    public string GroupObject { get; set; } = "";
    public string GroupProduct { get; set; } = "";
}
