using System;

namespace BanHangWeb.Models
{
    public class SupportMessage
    {
        public int Id { get; set; }

        public string User { get; set; }   // người gửi (email / tên)
        public string? ToUser { get; set; }
        public string Message { get; set; } // nội dung
        public DateTime SentAt { get; set; } // thời gian gửi
        public bool IsImage { get; set; } = false;
    }
}
