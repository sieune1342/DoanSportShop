using BanHangWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BanHangWeb.Hubs
{
    public class SupportHub : Hub
    {
        private readonly ApplicationDbContext _context;

        // connectionId -> Role (Admin / Customer)
        private static readonly ConcurrentDictionary<string, string> _connectionRoles = new();
        // connectionId -> UserName (email)
        private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();
        // userName -> set(connectionId)
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

        public SupportHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.IsInRole("Admin") == true ? "Admin" : "Customer";
            var userName = Context.User?.Identity?.Name ?? Context.ConnectionId;

            _connectionRoles[Context.ConnectionId] = role;
            _connectionUsers[Context.ConnectionId] = userName;

            _userConnections.AddOrUpdate(
                userName,
                _ => new HashSet<string> { Context.ConnectionId },
                (_, set) =>
                {
                    lock (set)
                    {
                        set.Add(Context.ConnectionId);
                    }
                    return set;
                });

            Console.WriteLine($"[Hub] {Context.ConnectionId} connected as {role}, user = {userName}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _connectionRoles.TryRemove(Context.ConnectionId, out _);

            if (_connectionUsers.TryRemove(Context.ConnectionId, out var userName))
            {
                if (_userConnections.TryGetValue(userName, out var set))
                {
                    lock (set)
                    {
                        set.Remove(Context.ConnectionId);
                    }

                    if (set.Count == 0)
                    {
                        _userConnections.TryRemove(userName, out _);
                    }
                }
            }

            Console.WriteLine($"[Hub] {Context.ConnectionId} disconnected");
            await base.OnDisconnectedAsync(exception);
        }

        // ====== LOAD LỊCH SỬ CHAT ======
        public async Task LoadHistory()
        {
            var isAdmin = Context.User?.IsInRole("Admin") == true;
            var userName = Context.User?.Identity?.Name ?? Context.ConnectionId;

            var raw = await _context.SupportMessages
                .OrderBy(m => m.SentAt)
                .Take(200)
                .ToListAsync();

            var mapped = new List<(string Sender, string Customer, string Message, DateTime SentAt, bool IsImage)>();

            // ⚠️ giống email admin bạn seed trong Program.cs
            const string AdminEmail = "Admin@gmail.com";

            string currentCustomer = null;

            foreach (var m in raw)
            {
                var sender = m.User;
                bool senderIsAdmin = !string.IsNullOrEmpty(sender) &&
                                     sender.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase);

                if (!senderIsAdmin)
                {
                    currentCustomer = sender;
                    mapped.Add((sender, currentCustomer, m.Message, m.SentAt, m.IsImage));
                }
                else if (currentCustomer != null)
                {
                    mapped.Add((sender, currentCustomer, m.Message, m.SentAt, m.IsImage));
                }
            }

            IEnumerable<object> toSend;

            if (!isAdmin)
            {
                // KHÁCH: chỉ thấy hội thoại của chính mình
                if (string.IsNullOrEmpty(userName))
                {
                    await Clients.Caller.SendAsync("LoadHistory", Array.Empty<object>());
                    return;
                }

                toSend = mapped
                    .Where(x =>
                        x.Customer != null &&
                        x.Customer.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    .Select(x => new
                    {
                        user = x.Sender,
                        message = x.Message,
                        sentAt = x.SentAt,
                        isImage = x.IsImage
                    });
            }
            else
            {
                // ADMIN: full, client tự nhóm theo từng customer
                toSend = mapped.Select(x => new
                {
                    user = x.Sender,
                    message = x.Message,
                    sentAt = x.SentAt,
                    isImage = x.IsImage
                });
            }

            await Clients.Caller.SendAsync("LoadHistory", toSend);
        }

        // ====== CHAT TEXT 1–1 ======
        public async Task SendMessage(string fromUser, string toUser, string message)
        {
            var msg = new SupportMessage
            {
                User = fromUser,
                Message = message,
                IsImage = false,
                SentAt = DateTime.UtcNow
            };

            _context.SupportMessages.Add(msg);
            await _context.SaveChangesAsync();

            _connectionRoles.TryGetValue(Context.ConnectionId, out var role);
            role ??= "Customer";

            var targetConnectionIds = new List<string>();

            if (role == "Admin")
            {
                if (!string.IsNullOrEmpty(toUser) &&
                    _userConnections.TryGetValue(toUser, out var connSet))
                {
                    lock (connSet)
                    {
                        targetConnectionIds.AddRange(connSet);
                    }
                }
            }
            else
            {
                foreach (var kv in _connectionRoles.Where(kv => kv.Value == "Admin"))
                {
                    targetConnectionIds.Add(kv.Key);
                }
            }

            if (targetConnectionIds.Count == 0)
            {
                Console.WriteLine("[Hub] No target connection for text message.");
                return;
            }

            await Clients.Clients(targetConnectionIds)
                .SendAsync("ReceiveMessage", fromUser, message);
        }

        // ====== CHAT ẢNH 1–1 ======
        public async Task SendImage(string fromUser, string toUser, string imageUrl)
        {
            var msg = new SupportMessage
            {
                User = fromUser,
                Message = imageUrl,
                IsImage = true,
                SentAt = DateTime.UtcNow
            };

            _context.SupportMessages.Add(msg);
            await _context.SaveChangesAsync();

            _connectionRoles.TryGetValue(Context.ConnectionId, out var role);
            role ??= "Customer";

            var targetConnectionIds = new List<string>();

            if (role == "Admin")
            {
                if (!string.IsNullOrEmpty(toUser) &&
                    _userConnections.TryGetValue(toUser, out var connSet))
                {
                    lock (connSet)
                    {
                        targetConnectionIds.AddRange(connSet);
                    }
                }
            }
            else
            {
                foreach (var kv in _connectionRoles.Where(kv => kv.Value == "Admin"))
                {
                    targetConnectionIds.Add(kv.Key);
                }
            }

            if (targetConnectionIds.Count == 0)
            {
                Console.WriteLine("[Hub] No target connection for image.");
                return;
            }

            await Clients.Clients(targetConnectionIds)
                .SendAsync("ReceiveImage", fromUser, imageUrl);
        }

        // ====== ĐÓNG HỘI THOẠI ======
        public async Task CloseConversation(string customerEmail)
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
                return;

            var messages = await _context.SupportMessages
                .Where(m => m.User == customerEmail)
                .ToListAsync();

            if (messages.Any())
            {
                _context.SupportMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }

            await Clients.Caller.SendAsync("ConversationClosed", customerEmail);
        }

        // ====== HÀM PHỤ CHO WEBRTC 1–1 ======

        private IEnumerable<string> GetConnectionsOfUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Array.Empty<string>();

            if (_userConnections.TryGetValue(userName, out var set))
            {
                lock (set)
                {
                    return set.ToList();
                }
            }

            return Array.Empty<string>();
        }

        private IEnumerable<string> GetAdminConnections()
        {
            return _connectionRoles
                .Where(kv => kv.Value == "Admin")
                .Select(kv => kv.Key)
                .ToList();
        }

        // ====== WEBRTC 1–1 ======

        // offer: fromUser -> toUser (Admin) / -> Admin (Customer)
        public async Task SendOffer(string offer, string fromUser, string toUser)
        {
            _connectionRoles.TryGetValue(Context.ConnectionId, out var role);
            role ??= "Customer";

            IEnumerable<string> targets;

            if (role == "Admin")
            {
                // Admin gọi đúng 1 khách
                targets = GetConnectionsOfUser(toUser);
            }
            else
            {
                // Khách gọi lên -> tất cả Admin
                targets = GetAdminConnections();
            }

            if (!targets.Any())
            {
                Console.WriteLine("[WebRTC] No targets for offer.");
                return;
            }

            await Clients.Clients(targets)
                .SendAsync("ReceiveOffer", offer, fromUser, toUser);
        }

        public async Task SendAnswer(string answer, string fromUser, string toUser)
        {
            _connectionRoles.TryGetValue(Context.ConnectionId, out var role);
            role ??= "Customer";

            IEnumerable<string> targets;

            if (role == "Admin")
            {
                // Admin trả lời khách -> gửi cho khách đó
                targets = GetConnectionsOfUser(toUser);
            }
            else
            {
                // Khách trả lời -> gửi cho tất cả Admin
                targets = GetAdminConnections();
            }

            if (!targets.Any())
            {
                Console.WriteLine("[WebRTC] No targets for answer.");
                return;
            }

            await Clients.Clients(targets)
                .SendAsync("ReceiveAnswer", answer, fromUser, toUser);
        }

        public async Task SendIceCandidate(string candidate, string fromUser, string toUser)
        {
            _connectionRoles.TryGetValue(Context.ConnectionId, out var role);
            role ??= "Customer";

            IEnumerable<string> targets;

            if (role == "Admin")
            {
                targets = GetConnectionsOfUser(toUser);
            }
            else
            {
                targets = GetAdminConnections();
            }

            if (!targets.Any())
            {
                Console.WriteLine("[WebRTC] No targets for ICE.");
                return;
            }

            await Clients.Clients(targets)
                .SendAsync("ReceiveIceCandidate", candidate, fromUser, toUser);
        }
    }
}
