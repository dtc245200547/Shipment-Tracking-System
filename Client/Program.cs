using System.Net;
using System.Net.Http.Json;

using var client = new HttpClient();
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
client.BaseAddress = new Uri(baseUrl);

Console.WriteLine("--- BUOC 1: LAY DANH SACH LO HANG ---");
var shipments = await client.GetFromJsonAsync<List<ShipmentDto>>("/api/shipments");
if (shipments != null)
{
    foreach (var s in shipments)
    {
        Console.WriteLine($"- Id: {s.Id}, Ma Van Don: {s.TrackingNumber}, Tuyen: {s.Route}, Phi: {s.Cost}, Trang thai: {s.Status}");
    }
}

Console.WriteLine("\n--- BUOC 2: TAO MOI LO HANG HOP LE ---");
var newShipmentReq = new CreateShipmentRequest("SP004", "Đà Nẵng - Nha Trang", 120000);
var createResponse = await client.PostAsJsonAsync("/api/shipments", newShipmentReq);

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
var detailResponse = await client.GetAsync("/api/shipments/1");
if (detailResponse.IsSuccessStatusCode)
{
    var detail = await detailResponse.Content.ReadFromJsonAsync<ShipmentDto>();
    Console.WriteLine($"Chi tiet: {detail?.TrackingNumber} - {detail?.Route}");
}

Console.WriteLine("\n--- BUOC 4: Kiem tra POST LỖI (Gia am) ---");
var badReq = new CreateShipmentRequest("SP005", "Sai Gon - Ha Noi", -50000);
var badResponse = await client.PostAsJsonAsync("/api/shipments", badReq);
if (badResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var error = await badResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"[Tu choi] {error?.Code}: {error?.Message}");
}

public record ShipmentDto(int Id, string TrackingNumber, string? Route, decimal Cost, string Status);
public record CreateShipmentRequest(string TrackingNumber, string? Route, decimal Cost);
public record ApiError(string Code, string Message);