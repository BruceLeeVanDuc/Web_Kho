namespace OlapWebBTL.Services;

/// <summary>
/// Tập hợp tên Measures / Dimensions / Attributes thật trong cube
/// "DW Quan Ly Phan Tich" (đã verify qua Diagnostics schema).
/// </summary>
public static class MdxQueries
{
    public const string Cube = "[DW Quan Ly Phan Tich]";

    // ============ MEASURES ============
    public const string MTongTien = "[Measures].[Tong Tien]";             // Doanh thu
    public const string MSoLuongDat = "[Measures].[So Luong Dat]";        // SL đặt
    public const string MDatCount = "[Measures].[Fact Dat Hang Count]";   // Số dòng fact đặt
    public const string MSoLuongTrongKho = "[Measures].[So Luong Trong Kho]";
    public const string MLuuKhoCount = "[Measures].[Fact Luu Kho Count]";

    // Alias cho tương thích code cũ
    public const string MGiaDat = MTongTien;

    // ============ DIM THOI GIAN ============
    public const string DimThoiGian = "[Dim Thoi Gian]";
    public const string ThoiGianNam = "[Dim Thoi Gian].[Nam]";
    public const string ThoiGianQuy = "[Dim Thoi Gian].[Quy]";
    public const string ThoiGianThang = "[Dim Thoi Gian].[Thang]";
    public const string ThoiGianNgay = "[Dim Thoi Gian].[Ngay]";

    // ============ DIM MAT HANG ============
    public const string DimMatHang = "[Dim Mat Hang]";
    public const string MatHangMa = "[Dim Mat Hang].[Ma MH]";
    public const string MatHangMoTa = "[Dim Mat Hang].[Mo Ta]";
    public const string MatHangKichCo = "[Dim Mat Hang].[Kich Co]";
    public const string MatHangTrongLuong = "[Dim Mat Hang].[Trong Luong]";
    public const string MatHangGia = "[Dim Mat Hang].[Gia Niem Yet]";

    // ============ DIM KHACH HANG ============
    public const string DimKhachHang = "[Dim Khach Hang]";
    public const string KhachHangMa = "[Dim Khach Hang].[Ma KH]";
    public const string KhachHangTen = "[Dim Khach Hang].[Ten KH]";
    public const string KhachHangLoai = "[Dim Khach Hang].[Loai Khach Hang]";
    public const string KhachHangThanhPho = "[Dim Khach Hang].[Ten Thanh Pho]";
    public const string KhachHangBang = "[Dim Khach Hang].[Bang]";
    public const string KhachHangNgayDauTien = "[Dim Khach Hang].[Ngay Dat Hang Dau Tien]";
    public const string KhachHangHDV = "[Dim Khach Hang].[Huong Dan Vien]";
    public const string KhachHangBuuDien = "[Dim Khach Hang].[Dia Chi Buu Dien]";

    // ============ DIM CUA HANG - CHƯA CÓ TRONG CUBE! ============
    // Các attribute sau CHƯA được add vào cube - bạn cần mở SSDT, vào tab
    // "Cube Structure", kéo Dim Cua Hang vào, rồi vào "Dimension Usage" để
    // link Dim Cua Hang với Fact_DatHang và Fact_LuuKho qua Ma_CH, sau đó
    // deploy + process cube.
    public const string DimCuaHang = "[Dim Cua Hang]";
    public const string CuaHangMa = "[Dim Cua Hang].[Ma CH]";
    public const string CuaHangThanhPho = "[Dim Cua Hang].[Ten Thanh Pho]";
    public const string CuaHangBang = "[Dim Cua Hang].[Bang]";
    public const string CuaHangSdt = "[Dim Cua Hang].[So Dien Thoai]";
    public const string CuaHangDiaChi = "[Dim Cua Hang].[Dia Chi VPDD]";

    // ============ DIM DON DAT HANG - KHÔNG TỒN TẠI! ============
    // Cube hiện chỉ có 3 dimension: Khach Hang, Mat Hang, Thoi Gian.
    // Nếu muốn làm báo cáo theo "Mã đơn", bạn phải tạo thêm dimension
    // "Dim Don Dat Hang" từ bảng đơn hàng hoặc fact dimension trên Ma_Don.
    public const string DimDonHang = "[Dim Don Dat Hang]";
    public const string DonHangMa = "[Dim Don Dat Hang].[Ma Don]";
}
