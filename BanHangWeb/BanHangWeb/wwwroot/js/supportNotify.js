"use strict";

// Kết nối riêng để đếm thông báo
let notifyConnection = null;
let supportUnread = 0;

let badgeNav = null;   // badge trên chữ "Quản trị"
let badgeMenu = null;  // badge trên dòng "Hỗ trợ khách hàng"

function updateSupportBadges() {
    if (!badgeNav) badgeNav = document.getElementById("supportBadgeNav");
    if (!badgeMenu) badgeMenu = document.getElementById("supportBadgeMenu");

    const text = supportUnread > 99 ? "99+" : supportUnread.toString();

    [badgeNav, badgeMenu].forEach(badge => {
        if (!badge) return;

        if (supportUnread > 0) {
            badge.classList.remove("d-none");
            badge.textContent = text;
        } else {
            badge.classList.add("d-none");
            badge.textContent = "";
        }
    });
}

function loadUnreadFromStorage() {
    try {
        const saved = localStorage.getItem("supportUnread");
        if (saved) {
            const n = parseInt(saved, 10);
            if (!isNaN(n) && n >= 0) {
                supportUnread = n;
            }
        }
    } catch (e) {
        console.warn("Không đọc được supportUnread từ localStorage", e);
    }
    updateSupportBadges();
}

function saveUnreadToStorage() {
    try {
        localStorage.setItem("supportUnread", supportUnread.toString());
    } catch (e) {
        console.warn("Không lưu được supportUnread vào localStorage", e);
    }
}

// Hàm cho trang Support/Admin gọi để reset về 0
function resetSupportUnread() {
    supportUnread = 0;
    saveUnreadToStorage();
    updateSupportBadges();
}

// Đưa ra global để dùng ở file khác (support.js)
window.resetSupportUnread = resetSupportUnread;

document.addEventListener("DOMContentLoaded", function () {
    // Layout này chỉ nhúng cho Admin, nhưng check cho chắc
    const currentUser = window.CURRENT_USER_NAME || "";
    if (!currentUser) return;

    badgeNav = document.getElementById("supportBadgeNav");
    badgeMenu = document.getElementById("supportBadgeMenu");

    loadUnreadFromStorage();

    if (typeof signalR === "undefined") {
        console.error("signalR chưa được load – kiểm tra lại CDN trong _Layout.cshtml");
        return;
    }

    notifyConnection = new signalR.HubConnectionBuilder()
        .withUrl("/supportHub")
        .withAutomaticReconnect()
        .build();

    // Mỗi khi nhận được tin nhắn mới
    notifyConnection.on("ReceiveMessage", (fromUser, message) => {
        // Nếu là tin nhắn do chính Admin gửi thì bỏ qua
        if (fromUser === currentUser) return;

        // → tin nhắn từ KHÁCH: tăng số chưa đọc
        supportUnread++;
        saveUnreadToStorage();
        updateSupportBadges();
    });

    notifyConnection.start().catch(err => {
        console.error("Không kết nối được notifyConnection:", err);
    });
});
