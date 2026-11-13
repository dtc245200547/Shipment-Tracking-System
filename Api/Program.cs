var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var shipments = new List<ShipmentDto>
{
    new ShipmentDto(1, "SP001", "Hà Nội - Đà Nẵng", 150000, "Đang xử lý"),
    new ShipmentDto(2, "SP002", "Hồ Chí Minh - Cần Thơ", 85000, "Đang vận chuyển"),
    new ShipmentDto(3, "SP003", "Hải Phòng - Hà Nội", 45000, "Đã giao hàng")
};

app.MapGet("/", () => Results.Ok(new { service = "Shipment Tracking API", status = "Running" }));

app.MapGet("/api/shipments", () => Results.Ok(shipments));

app.MapGet("/api/shipments/{id:int}", (int id) =>
{
    var shipment = shipments.FirstOrDefault(s => s.Id == id);
    if (shipment == null)
    {
        return Results.NotFound(new ApiError("SHIPMENT_NOT_FOUND", $"Không tìm thấy lô hàng với Id = {id}"));
    }
    return Results.Ok(shipment);
});

app.MapPost("/api/shipments", (CreateShipmentRequest request) =>
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
    
    return Results.Created($"/api/shipments/{nextId}", newShipment);
});

app.Run();

public record ShipmentDto(int Id, string TrackingNumber, string? Route, decimal Cost, string Status);
public record CreateShipmentRequest(string TrackingNumber, string? Route, decimal Cost);
public record ApiError(string Code, string Message);