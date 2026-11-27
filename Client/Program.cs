using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client; // Thêm thư viện SignalR

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5050";
var trackedCode = args.Length > 1 ? args[1] : "SP002";

// =======================================================
// PHẦN 1: KẾT NỐI SIGNALR (LẮNG NGHE THÔNG BÁO REAL-TIME)
// =======================================================
Console.WriteLine("--- KET NOI SIGNALR (NOTIFICATION SERVICE) ---");
await using var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/hubs/shipment")
    .WithAutomaticReconnect()
    .Build();

// Đăng ký nhận thông báo từ Server qua sự kiện "SystemAlert"
hubConnection.On<string>("SystemAlert", (message) =>
{
    // Đổi màu chữ sang xanh lá cây cho dễ nhìn khi có thông báo tới
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[REAL-TIME ALERT] {message}");
    Console.ResetColor();
});

// Đăng ký nhận thông báo theo nhóm mã vận đơn qua sự kiện "GroupAlert"
hubConnection.On<string>("GroupAlert", (message) =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n[GROUP ALERT] {message}");
    Console.ResetColor();
});

// Xử lý vòng đời kết nối trước khi StartAsync
hubConnection.Reconnecting += error =>
{
    Console.WriteLine($"\n[SignalR] Đang thử kết nối lại... ({error?.Message})");
    return Task.CompletedTask;
};

hubConnection.Reconnected += async connectionId =>
{
    Console.WriteLine($"\n[SignalR] Đã kết nối lại (Id: {connectionId}). Đăng ký lại nhóm {trackedCode}...");
    await hubConnection.InvokeAsync("JoinTrackingGroup", trackedCode);
};

hubConnection.Closed += error =>
{
    Console.WriteLine($"\n[SignalR] Kết nối đã đóng. {(error != null ? $"Lỗi: {error.Message}" : "")}");
    return Task.CompletedTask;
};

// Bắt đầu mở kết nối
await hubConnection.StartAsync();
Console.WriteLine("Da ket noi SignalR thanh cong!\n");

// Đăng ký theo dõi riêng lộ trình của lô hàng (Test chức năng Group)
await hubConnection.InvokeAsync("JoinTrackingGroup", trackedCode);
await hubConnection.InvokeAsync("SendToTrackingGroup", trackedCode, $"Client đang theo dõi {trackedCode}");


// =======================================================
// PHẦN 2: CÁC BƯỚC GỌI HTTP API CŨ (Giữ nguyên của bạn)
// =======================================================
using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri(baseUrl);

Console.WriteLine("\n--- BUOC 1: LAY DANH SACH LO HANG ---");
var shipments = await httpClient.GetFromJsonAsync<List<ShipmentDto>>("/api/shipments");
if (shipments != null)
{
    foreach (var s in shipments)
    {
        Console.WriteLine($"- Id: {s.Id}, Ma Van Don: {s.TrackingNumber}, Tuyen: {s.Route}, Phi: {s.Cost}, Trang thai: {s.Status}");
    }
}

Console.WriteLine("\n--- BUOC 2: TAO MOI LO HANG HOP LE (Sẽ kích hoạt SignalR) ---");
var newShipmentReq = new CreateShipmentRequest("SP004", "Đà Nẵng - Nha Trang", 120000);
var createResponse = await httpClient.PostAsJsonAsync("/api/shipments", newShipmentReq);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var createdShipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>();
    Console.WriteLine($"[Thanh cong] Da tao lo hang Id: {createdShipment?.Id}, Location: {createResponse.Headers.Location}");
}
else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"[Loi] {error?.Code}: {error?.Message}");
}

Console.WriteLine("\n--- BUOC 3: LAY CHI TIET LO HANG (Id = 1) ---");
var detailResponse = await httpClient.GetAsync("/api/shipments/1");
if (detailResponse.IsSuccessStatusCode)
{
    var detail = await detailResponse.Content.ReadFromJsonAsync<ShipmentDto>();
    Console.WriteLine($"Chi tiet: {detail?.TrackingNumber} - {detail?.Route}");
}

Console.WriteLine("\n--- BUOC 4: Kiem tra POST LỖI (Gia am) ---");
var badReq = new CreateShipmentRequest("SP005", "Sai Gon - Ha Noi", -50000);
var badResponse = await httpClient.PostAsJsonAsync("/api/shipments", badReq);
if (badResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var error = await badResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"[Tu choi] {error?.Code}: {error?.Message}");
}

// Lệnh này giúp màn hình Console không bị tắt ngay, 
// để máy khách tiếp tục "đứng chờ" nhận các thông báo Real-time tiếp theo
Console.WriteLine("\nAn phim bat ky de thoat chuong trinh...");
Console.ReadKey();

// --- DTOs ---
public record ShipmentDto(int Id, string TrackingNumber, string? Route, decimal Cost, string Status);
public record CreateShipmentRequest(string TrackingNumber, string? Route, decimal Cost);
public record ApiError(string Code, string Message);