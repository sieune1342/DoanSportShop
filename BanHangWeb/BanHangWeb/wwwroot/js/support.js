"use strict";

let connection = null;
let messagesDiv = null;
let messageInput = null;
let sendButton = null;

let localVideo = null;
let remoteVideo = null;
let startCallBtn = null;
let hangupBtn = null;

let localStream = null;
let peerConnection = null;

const iceConfig = {
    iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
};

/* ------- BIẾN RIÊNG CHO ADMIN (tách hội thoại) ------- */
let conversationListEl = null;
let conversationTitleEl = null;
let conversations = {};         // { userEmail: [ { from, text, isImage, time } ] }
let selectedUser = null;
let globalUnread = 0;           // số tin chưa đọc cho Admin

/* ========== HÀM PHỤ UNREAD (CHỈ GLOBAL) ========== */

function updateGlobalUnreadBadge() {
    const navBadge = document.getElementById("adminChatBadge");
    const menuBadge = document.getElementById("adminSupportMenuBadge");

    [navBadge, menuBadge].forEach(badge => {
        if (!badge) return;
        if (globalUnread > 0) {
            badge.textContent = globalUnread;
            badge.classList.remove("d-none");
        } else {
            badge.textContent = "";
            badge.classList.add("d-none");
        }
    });
}

/* ========== HÀM DÙNG CHUNG ========== */

function addMessageRow(container, from, text, isMe) {
    if (!container) return;

    const wrapper = document.createElement("div");
    wrapper.className = "mb-1 " + (isMe ? "text-end" : "text-start");

    const bubble = document.createElement("div");
    bubble.className =
        "d-inline-block px-2 py-1 rounded-3 " +
        (isMe ? "bg-primary text-white" : "bg-light border");

    bubble.textContent = text;

    wrapper.appendChild(bubble);
    container.appendChild(wrapper);

    container.scrollTop = container.scrollHeight;
}

function addImageRow(container, from, imageUrl, isMe) {
    if (!container) return;

    const wrapper = document.createElement("div");
    wrapper.className = "mb-1 " + (isMe ? "text-end" : "text-start");

    const bubble = document.createElement("div");
    bubble.className =
        "d-inline-block p-1 rounded-3 " +
        (isMe ? "bg-primary" : "bg-light border");

    const img = document.createElement("img");
    img.src = imageUrl;
    img.style.maxWidth = "180px";
    img.style.borderRadius = "0.5rem";
    img.alt = "image";

    bubble.appendChild(img);
    wrapper.appendChild(bubble);
    container.appendChild(wrapper);

    container.scrollTop = container.scrollHeight;
}

/* ----- ADMIN: quản lý hội thoại ----- */

function ensureConversation(user) {
    if (!conversations[user]) {
        conversations[user] = [];
    }

    if (conversationListEl) {
        let li = [...conversationListEl.children].find(li => li.dataset.user === user);
        if (!li) {
            li = document.createElement("li");
            li.className = "list-group-item list-group-item-action";
            li.dataset.user = user;
            li.textContent = user;
            li.addEventListener("click", () => selectConversation(user));
            conversationListEl.appendChild(li);
        }
    }
}

function renderConversationMessages() {
    if (!messagesDiv) return;
    messagesDiv.innerHTML = "";

    if (!selectedUser || !conversations[selectedUser]) return;

    const msgs = conversations[selectedUser];
    const meName = window.CURRENT_USER_NAME || "Admin";

    msgs.forEach(m => {
        const isMe = (m.from === meName);
        if (m.isImage) {
            addImageRow(messagesDiv, m.from, m.text, isMe);
        } else {
            addMessageRow(messagesDiv, m.from, m.text, isMe);
        }
    });

    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

function selectConversation(user) {
    selectedUser = user;

    const role = (window.SUPPORT_ROLE || "customer").toLowerCase();

    // 🔥 Admin chọn khách nào thì dùng khách đó làm đối tượng gọi video
    if (role === "admin") {
        window.VIDEO_TARGET_USER = user;
    }

    if (conversationTitleEl) {
        conversationTitleEl.textContent = "Hội thoại với " + user;
    }

    if (conversationListEl) {
        [...conversationListEl.children].forEach(li => {
            li.classList.toggle("active", li.dataset.user === user);
        });
    }

    if (role === "admin") {
        globalUnread = 0;
        updateGlobalUnreadBadge();
    }

    renderConversationMessages();
}


/* ========== SIGNALR ========== */

async function startSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/supportHub")
        .withAutomaticReconnect()
        .build();

    // Nhận lịch sử chat từ server
    connection.on("LoadHistory", (history) => {
        const role = (window.SUPPORT_ROLE || "customer").toLowerCase();
        const meName =
            window.CURRENT_USER_NAME ||
            (role === "admin" ? "Admin" : "Bạn");

        if (!Array.isArray(history)) return;

        if (role === "admin") {
            conversations = {};
            selectedUser = null;
            globalUnread = 0;
            if (conversationListEl) conversationListEl.innerHTML = "";
            if (messagesDiv) messagesDiv.innerHTML = "";
            updateGlobalUnreadBadge();

            let lastCustomer = null;

            history.forEach(m => {
                if (!m || !m.user || !m.message) return;

                const from = m.user;
                const isImg = m.isImage === true;

                if (from !== meName) {
                    // tin khách
                    lastCustomer = from;
                    ensureConversation(from);
                    conversations[from].push({
                        from: from,
                        text: m.message,
                        isImage: isImg,
                        time: m.sentAt ? new Date(m.sentAt) : new Date()
                    });
                } else {
                    // tin admin → gán cho khách gần nhất
                    if (!lastCustomer) return;
                    ensureConversation(lastCustomer);
                    conversations[lastCustomer].push({
                        from: meName,
                        text: m.message,
                        isImage: isImg,
                        time: m.sentAt ? new Date(m.sentAt) : new Date()
                    });
                }
            });

            const users = Object.keys(conversations);
            if (users.length > 0) {
                selectConversation(users[0]);
            }
        } else {
            // KHÁCH: server đã lọc chỉ hội thoại của chính mình
            if (!messagesDiv) return;
            messagesDiv.innerHTML = "";

            history.forEach(m => {
                if (!m || !m.user || !m.message) return;
                const isMe = (m.user === meName);
                const isImg = m.isImage === true;

                if (isImg) {
                    addImageRow(messagesDiv, m.user, m.message, isMe);
                } else {
                    addMessageRow(messagesDiv, m.user, m.message, isMe);
                }
            });
        }
    });

    // Nhận chat TEXT realtime
    connection.on("ReceiveMessage", (user, message) => {
        const role = (window.SUPPORT_ROLE || "customer").toLowerCase();

        if (role === "admin") {
            const meName = window.CURRENT_USER_NAME || "Admin";
            if (user === meName) return;

            ensureConversation(user);
            conversations[user].push({
                from: user,
                text: message,
                isImage: false,
                time: new Date()
            });

            if (!selectedUser || selectedUser !== user) {
                globalUnread++;
                updateGlobalUnreadBadge();
            } else {
                renderConversationMessages();
            }
        } else {
            if (!messagesDiv) return;
            const meName = window.CURRENT_USER_NAME || "Bạn";
            const isMe = (user === meName);
            addMessageRow(messagesDiv, user, message, isMe);
        }
    });

    // Nhận chat IMAGE realtime
    connection.on("ReceiveImage", (user, imageUrl) => {
        const role = (window.SUPPORT_ROLE || "customer").toLowerCase();

        if (role === "admin") {
            const meName = window.CURRENT_USER_NAME || "Admin";
            if (user === meName) return;

            ensureConversation(user);
            conversations[user].push({
                from: user,
                text: imageUrl,
                isImage: true,
                time: new Date()
            });

            if (!selectedUser || selectedUser !== user) {
                globalUnread++;
                updateGlobalUnreadBadge();
            } else {
                renderConversationMessages();
            }
        } else {
            if (!messagesDiv) return;
            const meName = window.CURRENT_USER_NAME || "Bạn";
            const isMe = (user === meName);
            addImageRow(messagesDiv, user, imageUrl, isMe);
        }
    });

    // Đóng hội thoại
    connection.on("ConversationClosed", (customerEmail) => {
        const role = (window.SUPPORT_ROLE || "customer").toLowerCase();

        if (role === "admin") {
            if (conversations[customerEmail]) {
                delete conversations[customerEmail];
            }

            if (conversationListEl) {
                const li = conversationListEl.querySelector(
                    `li[data-user="${customerEmail}"]`
                );
                if (li) li.remove();
            }

            if (selectedUser === customerEmail) {
                selectedUser = null;
                if (conversationTitleEl) {
                    conversationTitleEl.textContent = "Chưa chọn khách";
                }
                if (messagesDiv) {
                    messagesDiv.innerHTML = "";
                }
            }
        } else {
            const myName = window.CURRENT_USER_NAME || "";
            if (myName === customerEmail && messagesDiv) {
                messagesDiv.innerHTML = "";
                const info = document.createElement("div");
                info.className = "text-center text-muted small mt-2";
                info.textContent = "Cuộc trò chuyện đã được nhân viên đóng. Gửi tin nhắn mới để mở lại.";
                messagesDiv.appendChild(info);
            }
        }
    });

    // ===== WEBRTC – nhận tín hiệu 1–1 (khớp Hub mới: SendOffer/Answer/Ice(fromUser, toUser)) =====
    connection.on("ReceiveOffer", async (offerJson, fromUser, toUser) => {
        console.log("Nhận Offer từ", fromUser, "->", toUser);

        if (!peerConnection) {
            await createPeerConnection();
        }
        const offer = JSON.parse(offerJson);
        await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));

        if (!localStream) {
            await startLocalStream();
        }
        localStream.getTracks().forEach(t => peerConnection.addTrack(t, localStream));

        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);

        const role = (window.SUPPORT_ROLE || "customer").toLowerCase();
        const meName =
            window.CURRENT_USER_NAME ||
            (role === "admin" ? "Admin" : "Bạn");

        let targetUser = "";
        if (role === "admin") {
            // admin đang nhận offer từ khách → trả lời lại đúng khách
            targetUser = fromUser;
        } else {
            // khách nhận offer từ admin → trả lời lại admin (server sẽ route theo toUser rỗng hoặc cấu hình khác)
            targetUser = "";
        }

        await connection.invoke("SendAnswer", JSON.stringify(answer), meName, targetUser);
    });

    connection.on("ReceiveAnswer", async (answerJson, fromUser, toUser) => {
        console.log("Nhận Answer từ", fromUser, "->", toUser);
        if (!peerConnection) return;
        const answer = JSON.parse(answerJson);
        await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
    });

    connection.on("ReceiveIceCandidate", async (candidateJson, fromUser, toUser) => {
        if (!peerConnection) return;
        const candidate = JSON.parse(candidateJson);
        try {
            await peerConnection.addIceCandidate(candidate);
        } catch (err) {
            console.error("Lỗi add ICE", err);
        }
    });

    try {
        await connection.start();
        console.log("SignalR connected, state =", connection.state);

        try {
            await connection.invoke("LoadHistory");
        } catch (err) {
            console.error("Không load được lịch sử:", err);
        }
    } catch (err) {
        console.error("Không kết nối được SignalR:", err);
    }
}

/* ========== WEBRTC ========== */

async function startLocalStream() {
    try {
        localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        if (localVideo) {
            localVideo.srcObject = localStream;
        }
    } catch (err) {
        console.error("Không truy cập được camera/micro", err);
        alert("Không truy cập được camera hoặc micro. Hãy kiểm tra quyền.");
    }
}

async function createPeerConnection() {
    peerConnection = new RTCPeerConnection(iceConfig);

    peerConnection.onicecandidate = (event) => {
        if (event.candidate && connection) {
            const role = (window.SUPPORT_ROLE || "customer").toLowerCase();
            const meName =
                window.CURRENT_USER_NAME ||
                (role === "admin" ? "Admin" : "Bạn");

            let targetUser = "";
            if (role === "admin") {
                // admin gửi ICE cho đúng khách đang gọi
                targetUser = window.VIDEO_TARGET_USER || "";
            } else {
                // khách gửi ICE lên admin
                targetUser = "";
            }

            connection.invoke(
                "SendIceCandidate",
                JSON.stringify(event.candidate),
                meName,
                targetUser
            );
        }
    };

    peerConnection.ontrack = (event) => {
        if (remoteVideo) {
            remoteVideo.srcObject = event.streams[0];
        }
    };

    peerConnection.onconnectionstatechange = () => {
        console.log("Trạng thái WebRTC:", peerConnection.connectionState);
        if (["disconnected", "failed", "closed"].includes(peerConnection.connectionState)) {
            endCall();
        }
    };
}

async function startCall() {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert("Không kết nối được tới máy chủ chat. Hãy reload lại trang.");
        return;
    }

    const role = (window.SUPPORT_ROLE || "customer").toLowerCase();
    const meName =
        window.CURRENT_USER_NAME ||
        (role === "admin" ? "Admin" : "Bạn");

    let targetUser = "";

    if (role === "admin") {
        // Ưu tiên VIDEO_TARGET_USER, nếu chưa có thì dùng selectedUser
        targetUser = window.VIDEO_TARGET_USER || selectedUser || "";

        if (!targetUser) {
            alert("Hãy chọn một khách trong danh sách bên trái trước khi gọi video.");
            return;
        }
    } else {
        // Khách gọi lên admin – server sẽ route sang admin
        targetUser = "";
    }

    await createPeerConnection();

    if (!localStream) {
        await startLocalStream();
    }

    localStream.getTracks().forEach(t => peerConnection.addTrack(t, localStream));

    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);

    await connection.invoke("SendOffer", JSON.stringify(offer), meName, targetUser);
}


function endCall() {
    if (peerConnection) {
        peerConnection.close();
        peerConnection = null;
    }
    if (localStream) {
        localStream.getTracks().forEach(t => t.stop());
        localStream = null;
    }
    if (localVideo) localVideo.srcObject = null;
    if (remoteVideo) remoteVideo.srcObject = null;
}

/* ========== KHỞI TẠO TRANG ========== */

document.addEventListener("DOMContentLoaded", function () {
    // CHAT
    messagesDiv = document.getElementById("messages");
    messageInput = document.getElementById("messageInput");
    sendButton = document.getElementById("sendButton");

    // VIDEO
    localVideo = document.getElementById("localVideo");
    remoteVideo = document.getElementById("remoteVideo");
    startCallBtn = document.getElementById("startCallBtn");
    hangupBtn = document.getElementById("hangupBtn");

    conversationListEl = document.getElementById("conversationList");
    conversationTitleEl = document.getElementById("conversationTitle");

    const role = (window.SUPPORT_ROLE || "customer").toLowerCase();
    const isChatPage = !!messagesDiv;
    const isVideoPage = !!localVideo || !!remoteVideo;

    // Không phải trang hỗ trợ / trang video thì thôi
    if (!isChatPage && !isVideoPage) return;

    /* ==== GỬI ẢNH – chỉ trang chat ==== */
    if (isChatPage) {
        const btnSendImage = document.getElementById("btnSendImage");
        const imageInput = document.getElementById("imageInput");

        if (btnSendImage && imageInput) {
            btnSendImage.addEventListener("click", () => {
                imageInput.click();
            });

            imageInput.addEventListener("change", async () => {
                const file = imageInput.files[0];
                if (!file) return;

                const formData = new FormData();
                formData.append("file", file);

                try {
                    const resp = await fetch("/Support/UploadImage", {
                        method: "POST",
                        body: formData
                    });

                    if (!resp.ok) {
                        alert("Upload ảnh thất bại");
                        imageInput.value = "";
                        return;
                    }

                    const data = await resp.json();
                    if (!data.success) {
                        alert(data.error || "Upload ảnh thất bại");
                        imageInput.value = "";
                        return;
                    }

                    const imageUrl = data.url;
                    const meName =
                        window.CURRENT_USER_NAME ||
                        (role === "admin" ? "Admin" : "Bạn");

                    let toUser = "";

                    if (role === "admin") {
                        if (!selectedUser) {
                            alert("Hãy chọn một khách ở cột bên trái trước khi gửi ảnh.");
                            imageInput.value = "";
                            return;
                        }

                        toUser = selectedUser;

                        ensureConversation(selectedUser);
                        conversations[selectedUser].push({
                            from: meName,
                            text: imageUrl,
                            isImage: true,
                            time: new Date()
                        });
                        renderConversationMessages();
                    } else {
                        toUser = ""; // Hub route tới Admin
                        addImageRow(messagesDiv, meName, imageUrl, true);
                    }

                    if (connection && connection.state === signalR.HubConnectionState.Connected) {
                        await connection.invoke("SendImage", meName, toUser, imageUrl);
                    }
                } catch (err) {
                    console.error(err);
                    alert("Có lỗi khi upload ảnh");
                } finally {
                    imageInput.value = "";
                }
            });
        }

        // Nút ĐÓNG HỘI THOẠI (Admin + chat)
        const closeConvBtn = document.getElementById("btn-close-conversation");
        if (closeConvBtn && role === "admin") {
            closeConvBtn.addEventListener("click", async () => {
                if (!selectedUser) {
                    alert("Hãy chọn một khách trong danh sách bên trái.");
                    return;
                }

                if (!confirm(`Đóng hội thoại với ${selectedUser}? Toàn bộ tin nhắn sẽ bị xóa.`)) {
                    return;
                }

                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    try {
                        await connection.invoke("CloseConversation", selectedUser);
                    } catch (err) {
                        console.error("Lỗi gọi CloseConversation:", err);
                    }
                }

                if (conversations[selectedUser]) {
                    delete conversations[selectedUser];
                }

                if (conversationListEl) {
                    const li = conversationListEl.querySelector(
                        `li[data-user="${selectedUser}"]`
                    );
                    if (li) li.remove();
                }

                selectedUser = null;
                if (conversationTitleEl) {
                    conversationTitleEl.textContent = "Chưa chọn khách";
                }
                if (messagesDiv) {
                    messagesDiv.innerHTML = "";
                }
            });
        }

        // Nút "Làm mới" danh sách hội thoại (Admin)
        const refreshBtn = document.getElementById("refreshConversations");
        if (refreshBtn) {
            refreshBtn.addEventListener("click", async () => {
                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    try {
                        await connection.invoke("LoadHistory");
                    } catch (err) {
                        console.error("Không load được lịch sử:", err);
                    }
                }
            });
        }

        // Chat TEXT – chỉ trang chat
        if (sendButton && messageInput) {
            sendButton.addEventListener("click", () => {
                const msg = messageInput.value.trim();
                if (!msg) return;

                const meName =
                    window.CURRENT_USER_NAME ||
                    (role === "admin" ? "Admin" : "Bạn");

                let toUser = "";

                if (role === "admin") {
                    if (!selectedUser) {
                        alert("Hãy chọn một khách ở cột bên trái trước khi gửi.");
                        return;
                    }

                    toUser = selectedUser;

                    ensureConversation(selectedUser);
                    conversations[selectedUser].push({
                        from: meName,
                        text: msg,
                        isImage: false,
                        time: new Date()
                    });
                    renderConversationMessages();
                } else {
                    toUser = ""; // Hub route tới Admin
                    addMessageRow(messagesDiv, meName, msg, true);
                }

                messageInput.value = "";

                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("SendMessage", meName, toUser, msg)
                        .catch(err => console.error(err.toString()));
                }
            });

            messageInput.addEventListener("keypress", (e) => {
                if (e.key === "Enter") {
                    e.preventDefault();
                    sendButton.click();
                }
            });
        }
    }

    // Call (trang video hoặc chat nếu có nút)
    if (startCallBtn) startCallBtn.addEventListener("click", () => startCall());
    if (hangupBtn) hangupBtn.addEventListener("click", () => endCall());

    // Kết nối SignalR cho cả chat & video
    startSignalR();
});
