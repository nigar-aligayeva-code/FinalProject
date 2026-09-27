using HotelTravel.Application.DTOs.Rooms;

namespace HotelTravel.Application.Interfaces;

public interface IRoomService
{
    Task<List<GetRoomDto>> GetAllAsync();

    Task<GetRoomDto?> GetByIdAsync(int id);

    Task CreateAsync(CreateRoomDto dto);

    Task<bool> UpdateAsync(int id, UpdateRoomDto dto);

    Task<bool> DeleteAsync(int id);

    Task<List<GetRoomDto>> SearchAvailableRoomsAsync(
        RoomSearchDto search);
}