using System.Net;
using System.Net.Http.Json;
using GatewayService.Application;
using GatewayService.Domain;
namespace GatewayService.Infrastructure;
public sealed class BookingHttpClients(HttpClient http, IConfiguration cfg) : IBookingClients
{
    private string R => cfg["Services:Reservation"] ?? "http://localhost:8070"; private string P => cfg["Services:Payment"] ?? "http://localhost:8060"; private string L => cfg["Services:Loyalty"] ?? "http://localhost:8050";
    private async Task<T> Get<T>(string url, CancellationToken ct) => (await http.GetFromJsonAsync<T>(url, ct))!;
    public Task<HotelPage> GetHotelsAsync(int p, int s, CancellationToken ct) => Get<HotelPage>($"{R}/internal/hotels?page={p}&size={s}", ct);
    public async Task<Hotel?> GetHotelAsync(Guid u, CancellationToken ct) { var x = await http.GetAsync($"{R}/internal/hotels/{u}", ct); return x.StatusCode == HttpStatusCode.NotFound ? null : await x.Content.ReadFromJsonAsync<Hotel>(ct); }
    public Task<Loyalty> GetLoyaltyAsync(string u, CancellationToken ct) => Get<Loyalty>($"{L}/internal/loyalty/{Uri.EscapeDataString(u)}", ct);
    public async Task<Loyalty> ChangeLoyaltyAsync(string u, bool inc, CancellationToken ct) { var url = $"{L}/internal/loyalty/{Uri.EscapeDataString(u)}/reservations"; var x = inc ? await http.PostAsync(url, null, ct) : await http.DeleteAsync(url, ct); x.EnsureSuccessStatusCode(); return (await x.Content.ReadFromJsonAsync<Loyalty>(ct))!; }
    public async Task<Payment> CreatePaymentAsync(int p, CancellationToken ct) { var x = await http.PostAsJsonAsync($"{P}/internal/payments", new { price = p }, ct); x.EnsureSuccessStatusCode(); return (await x.Content.ReadFromJsonAsync<Payment>(ct))!; }
    public async Task<Payment?> GetPaymentAsync(Guid u, CancellationToken ct) { var x = await http.GetAsync($"{P}/internal/payments/{u}", ct); return x.StatusCode == HttpStatusCode.NotFound ? null : await x.Content.ReadFromJsonAsync<Payment>(ct); }
    public async Task CancelPaymentAsync(Guid u, CancellationToken ct) => (await http.DeleteAsync($"{P}/internal/payments/{u}", ct)).EnsureSuccessStatusCode();
    public Task<IReadOnlyList<Reservation>> GetReservationsAsync(string u, CancellationToken ct) => Get<IReadOnlyList<Reservation>>($"{R}/internal/reservations?username={Uri.EscapeDataString(u)}", ct);
    public async Task<Reservation?> GetReservationAsync(Guid u, CancellationToken ct) { var x = await http.GetAsync($"{R}/internal/reservations/{u}", ct); return x.StatusCode == HttpStatusCode.NotFound ? null : await x.Content.ReadFromJsonAsync<Reservation>(ct); }
    public async Task<Reservation> CreateReservationAsync(Guid id, string user, Guid pay, Guid hotel, DateOnly start, DateOnly end, CancellationToken ct) { var x = await http.PostAsJsonAsync($"{R}/internal/reservations", new { reservationUid = id, username = user, paymentUid = pay, hotelUid = hotel, startDate = start, endDate = end }, ct); x.EnsureSuccessStatusCode(); return (await x.Content.ReadFromJsonAsync<Reservation>(ct))!; }
    public async Task<bool> CancelReservationAsync(Guid u, string user, CancellationToken ct) => (await http.DeleteAsync($"{R}/internal/reservations/{u}?username={Uri.EscapeDataString(user)}", ct)).IsSuccessStatusCode;
}
