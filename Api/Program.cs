using Microsoft.AspNetCore.SignalR; // Thêm thư viện SignalR

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

var shipments = new List<ShipmentDto>
{
    new ShipmentDto(1, "SP001", "Hà Nội - Đà Nẵng", 150000, "Đang xử lý"),
    new ShipmentDto(2, "SP002", "Hồ Chí Minh - Cần Thơ", 85000, "Đang vận chuyển"),
    new ShipmentDto(3, "SP003", "Hải Phòng - Hà Nội", 45000, "Đã giao hàng")
};

app.MapGet("/", () => Results.Ok(new { service = "Shipment Tracking API", status = "Running" }));

app.MapGet("/api/shipments", (string? keyword) =>
{
    // Nếu không truyền keyword, trả về toàn bộ danh sách
    if (string.IsNullOrWhiteSpace(keyword))
    {
        return Results.Ok(shipments);
    }
    
    // Nếu có keyword, lọc ra các lô hàng có mã vận đơn (TrackingNumber) chứa từ khóa đó
    var filteredShipments = shipments
        .Where(s => s.TrackingNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        .ToList();
        
    return Results.Ok(filteredShipments);
});

app.MapGet("/api/shipments/{id:int}", (int id) =>
{
    var shipment = shipments.FirstOrDefault(s => s.Id == id);
    if (shipment == null)
    {
        return Results.NotFound(new ApiError("SHIPMENT_NOT_FOUND", $"Không tìm thấy lô hàng với Id = {id}"));
    }
    return Results.Ok(shipment);
});

// Tiêm IHubContext vào POST để phát sự kiện SignalR
app.MapPost("/api/shipments", async (CreateShipmentRequest request, IHubContext<ShipmentHub> hubContext) =>
{
    if (string.IsNullOrWhiteSpace(request.TrackingNumber) || request.Cost <= 0)
    {
        return Results.BadRequest(new ApiError("INVALID_SHIPMENT", "Mã vận đơn không hợp lệ hoặc chi phí phải lớn hơn 0"));
    }

    var nextId = shipments.Any() ? shipments.Max(s => s.Id) + 1 : 1;
    var newShipment = new ShipmentDto(
        nextId, 
        request.TrackingNumber.Trim(), 
        request.Route?.Trim(), 
        request.Cost,
        "Đang xử lý" // Trạng thái mặc định khi tạo mới
    );
    
    shipments.Add(newShipment);
    
    // Phát thông báo Real-time (Broadcast) cho tất cả các Client
    await hubContext.Clients.All.SendAsync("SystemAlert", $"[Hệ thống] Lô hàng mới {newShipment.TrackingNumber} vừa được khởi tạo!");
    
    return Results.Created($"/api/shipments/{nextId}", newShipment);
});

// Ánh xạ endpoint cho SignalR Hub
app.MapHub<ShipmentHub>("/hubs/shipment");

app.Run();

// --- Lớp Hub quản lý kết nối SignalR ---
public class ShipmentHub : Hub
{
    // Chạy khi có một máy khách kết nối vào
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("SystemAlert", $"[Hệ thống] Một client vừa kết nối: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    // Cho phép máy khách đăng ký theo dõi 1 mã vận đơn cụ thể (Group)
    public async Task JoinTrackingGroup(string trackingNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, trackingNumber);
        await Clients.Caller.SendAsync("SystemAlert", $"[Thông báo] Bạn đã đăng ký nhận thông báo cho mã vận đơn: {trackingNumber}");
    }
}

// --- DTOs ---
public record ShipmentDto(int Id, string TrackingNumber, string? Route, decimal Cost, string Status);
public record CreateShipmentRequest(string TrackingNumber, string? Route, decimal Cost);
public record ApiError(string Code, string Message);