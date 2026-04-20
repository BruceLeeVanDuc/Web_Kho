using Microsoft.AspNetCore.Mvc;
using OlapWebBTL.Services;
using static OlapWebBTL.Services.MdxQueries;

namespace OlapWebBTL.Controllers;

public class ReportsController : Controller
{
    private readonly OlapService _olap;
    public ReportsController(OlapService olap) => _olap = olap;

    public IActionResult Index() => View();

    // ========================================================
    // Câu 1: Tìm tất cả cửa hàng cùng thành phố, bang, SĐT,
    // mô tả, kích cỡ, trọng lượng, đơn giá của các mặt hàng bán ở kho đó.
    // ========================================================
    public IActionResult Report1()
    {
        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongTrongKho} }} ON COLUMNS,
  NON EMPTY (
    {CuaHangMa}.[Ma CH].Members *
    {CuaHangThanhPho}.[Ten Thanh Pho].Members *
    {CuaHangBang}.[Bang].Members *
    {CuaHangSdt}.[So Dien Thoai].Members *
    {MatHangMoTa}.[Mo Ta].Members *
    {MatHangKichCo}.[Kich Co].Members *
    {MatHangTrongLuong}.[Trong Luong].Members *
    {MatHangGia}.[Gia Niem Yet].Members
  ) ON ROWS
FROM {Cube}";
        var r = _olap.ExecuteAsTable(mdx,
            "Câu 1: Cửa hàng + mặt hàng đang lưu trữ / bán");
        return View("Report", r);
    }

    // ========================================================
    // Câu 2: Tất cả đơn đặt hàng với tên khách hàng và ngày đặt
    // ========================================================
    public IActionResult Report2()
    {
        var mdx = $@"
SELECT
  NON EMPTY {{ {MGiaDat}, {MSoLuongDat} }} ON COLUMNS,
  NON EMPTY (
    {DonHangMa}.[Ma Don].Members *
    {KhachHangTen}.[Ten KH].Members *
    {ThoiGianNgay}.[Ngay].Members
  ) ON ROWS
FROM {Cube}";
        var r = _olap.ExecuteAsTable(mdx,
            "Câu 2: Đơn hàng + khách hàng + ngày đặt");
        return View("Report", r);
    }

    // ========================================================
    // Câu 3: Cửa hàng + thành phố + SĐT đang bán các mặt hàng
    // được đặt bởi một khách hàng cụ thể.
    // ========================================================
    public IActionResult Report3(string maKH = "")
    {
        var where = string.IsNullOrEmpty(maKH)
            ? ""
            : $"WHERE ({KhachHangMa}.&[{maKH}])";
        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongDat}, {MSoLuongTrongKho} }} ON COLUMNS,
  NON EMPTY (
    {CuaHangMa}.[Ma CH].Members *
    {CuaHangThanhPho}.[Ten Thanh Pho].Members *
    {CuaHangSdt}.[So Dien Thoai].Members *
    {MatHangMa}.[Ma MH].Members *
    {MatHangMoTa}.[Mo Ta].Members
  ) ON ROWS
FROM {Cube}
{where}";
        ViewBag.Param = "Mã KH";
        ViewBag.ParamValue = maKH;
        var r = _olap.ExecuteAsTable(mdx,
            $"Câu 3: Cửa hàng bán các mặt hàng của khách {(string.IsNullOrEmpty(maKH) ? "(tất cả)" : maKH)}");
        return View("Report", r);
    }

    // ========================================================
    // Câu 4: Địa chỉ VPĐD + thành phố + bang của cửa hàng
    // lưu một mặt hàng nào đó với số lượng > X.
    // ========================================================
    public IActionResult Report4(string maMH = "", int minQty = 100)
    {
        var where = string.IsNullOrEmpty(maMH)
            ? ""
            : $"WHERE ({MatHangMa}.&[{maMH}])";
        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongTrongKho} }} ON COLUMNS,
  NON EMPTY
    FILTER(
      {CuaHangMa}.[Ma CH].Members *
      {CuaHangThanhPho}.[Ten Thanh Pho].Members *
      {CuaHangBang}.[Bang].Members *
      {CuaHangDiaChi}.[Dia Chi VPDD].Members,
      {MSoLuongTrongKho} > {minQty}
    ) ON ROWS
FROM {Cube}
{where}";
        ViewBag.Param = "Mã MH / Min Qty";
        ViewBag.ParamValue = $"{maMH} / {minQty}";
        var r = _olap.ExecuteAsTable(mdx,
            $"Câu 4: VPĐD có lưu mặt hàng {(string.IsNullOrEmpty(maMH) ? "(tất cả)" : maMH)} > {minQty}");
        return View("Report", r);
    }

    // ========================================================
    // Câu 5: Mỗi đơn - mặt hàng đặt + mô tả + cửa hàng + thành phố
    // ========================================================
    public IActionResult Report5(string maDon = "")
    {
        var where = string.IsNullOrEmpty(maDon)
            ? ""
            : $"WHERE ({DonHangMa}.&[{maDon}])";
        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongDat}, {MGiaDat} }} ON COLUMNS,
  NON EMPTY (
    {DonHangMa}.[Ma Don].Members *
    {MatHangMa}.[Ma MH].Members *
    {MatHangMoTa}.[Mo Ta].Members *
    {CuaHangMa}.[Ma CH].Members *
    {CuaHangThanhPho}.[Ten Thanh Pho].Members
  ) ON ROWS
FROM {Cube}
{where}";
        ViewBag.Param = "Mã Đơn";
        ViewBag.ParamValue = maDon;
        var r = _olap.ExecuteAsTable(mdx,
            $"Câu 5: Chi tiết đơn hàng {(string.IsNullOrEmpty(maDon) ? "(tất cả)" : maDon)}");
        return View("Report", r);
    }

    // ========================================================
    // Câu 6: Thành phố và bang mà một KH sinh sống
    // ========================================================
    public IActionResult Report6(string maKH = "")
    {
        var where = string.IsNullOrEmpty(maKH)
            ? ""
            : $"WHERE ({KhachHangMa}.&[{maKH}])";
        var mdx = $@"
SELECT
  NON EMPTY {{ {MDatCount} }} ON COLUMNS,
  NON EMPTY (
    {KhachHangMa}.[Ma KH].Members *
    {KhachHangTen}.[Ten KH].Members *
    {KhachHangThanhPho}.[Ten Thanh Pho].Members *
    {KhachHangBang}.[Bang].Members
  ) ON ROWS
FROM {Cube}
{where}";
        ViewBag.Param = "Mã KH";
        ViewBag.ParamValue = maKH;
        var r = _olap.ExecuteAsTable(mdx,
            $"Câu 6: Thành phố / Bang của KH {(string.IsNullOrEmpty(maKH) ? "(tất cả)" : maKH)}");
        return View("Report", r);
    }

    // ========================================================
    // Câu 7: Mức tồn kho của 1 mặt hàng tại TẤT CẢ cửa hàng
    // của một thành phố cụ thể.
    // ========================================================
    public IActionResult Report7(string maMH = "", string thanhPho = "")
    {
        var filters = new List<string>();
        if (!string.IsNullOrEmpty(maMH)) filters.Add($"{MatHangMa}.&[{maMH}]");
        if (!string.IsNullOrEmpty(thanhPho)) filters.Add($"{CuaHangThanhPho}.&[{thanhPho}]");
        var where = filters.Count == 0 ? "" : $"WHERE ({string.Join(",", filters)})";

        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongTrongKho} }} ON COLUMNS,
  NON EMPTY (
    {CuaHangMa}.[Ma CH].Members *
    {CuaHangThanhPho}.[Ten Thanh Pho].Members *
    {MatHangMa}.[Ma MH].Members *
    {MatHangMoTa}.[Mo Ta].Members
  ) ON ROWS
FROM {Cube}
{where}";
        ViewBag.Param = "Mã MH / Thành phố";
        ViewBag.ParamValue = $"{maMH} / {thanhPho}";
        var r = _olap.ExecuteAsTable(mdx,
            $"Câu 7: Tồn kho mặt hàng {(string.IsNullOrEmpty(maMH) ? "(tất cả)" : maMH)} tại TP {(string.IsNullOrEmpty(thanhPho) ? "(tất cả)" : thanhPho)}");
        return View("Report", r);
    }

    // ========================================================
    // Câu 8: Đơn hàng - mặt hàng - số lượng - KH - cửa hàng - TP
    // ========================================================
    public IActionResult Report8(string maDon = "")
    {
        var where = string.IsNullOrEmpty(maDon)
            ? ""
            : $"WHERE ({DonHangMa}.&[{maDon}])";
        var mdx = $@"
SELECT
  NON EMPTY {{ {MSoLuongDat}, {MGiaDat} }} ON COLUMNS,
  NON EMPTY (
    {DonHangMa}.[Ma Don].Members *
    {MatHangMa}.[Ma MH].Members *
    {MatHangMoTa}.[Mo Ta].Members *
    {KhachHangTen}.[Ten KH].Members *
    {CuaHangMa}.[Ma CH].Members *
    {CuaHangThanhPho}.[Ten Thanh Pho].Members
  ) ON ROWS
FROM {Cube}
{where}";
        ViewBag.Param = "Mã Đơn";
        ViewBag.ParamValue = maDon;
        var r = _olap.ExecuteAsTable(mdx,
            $"Câu 8: Chi tiết đơn hàng {(string.IsNullOrEmpty(maDon) ? "(tất cả)" : maDon)}");
        return View("Report", r);
    }

    // ========================================================
    // Câu 9: Khách du lịch / bưu điện / cả 2 loại
    // ========================================================
    public IActionResult Report9()
    {
        var mdx = $@"
SELECT
  NON EMPTY {{ {MDatCount}, {MGiaDat} }} ON COLUMNS,
  NON EMPTY (
    {KhachHangLoai}.[Loai Khach Hang].Members *
    {KhachHangMa}.[Ma KH].Members *
    {KhachHangTen}.[Ten KH].Members
  ) ON ROWS
FROM {Cube}";
        var r = _olap.ExecuteAsTable(mdx,
            "Câu 9: Phân loại khách hàng (Du lịch / Bưu điện / Cả hai)");
        return View("Report", r);
    }
}
