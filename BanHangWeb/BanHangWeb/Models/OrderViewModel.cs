using System;

public class OrderViewModel
{
    public int OderId { get; set; }          // Mã đơn hàng
    public DateTime OrderDate { get; set; }  // Ngày đặt hàng
    public decimal TotalAmount { get; set; } // Tổng tiền
    public string Status { get; set; }       // Trạng thái đơn hàng
}
