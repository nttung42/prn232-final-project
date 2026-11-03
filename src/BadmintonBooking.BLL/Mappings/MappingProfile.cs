using AutoMapper;
using BadmintonBooking.BLL.DTOs.Auth;
using BadmintonBooking.BLL.DTOs.Bookings;
using BadmintonBooking.BLL.DTOs.Courts;
using BadmintonBooking.BLL.DTOs.Facilities;
using BadmintonBooking.BLL.DTOs.TimeSlots;
using BadmintonBooking.BLL.DTOs.Users;
using BadmintonBooking.BLL.DTOs.Wallet;
using BadmintonBooking.Domain.Entities;

namespace BadmintonBooking.BLL.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User Mappings
        CreateMap<User, UserDto>();
        CreateMap<RegisterRequestDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        CreateMap<UpdateUserDto, User>();

        // Facility Mappings
        CreateMap<Facility, FacilityDto>()
            .ForMember(dest => dest.ProviderName, opt => opt.MapFrom(src => src.Provider != null ? src.Provider.FullName : string.Empty))
            .ForMember(dest => dest.TotalCourts, opt => opt.MapFrom(src => src.Courts != null ? src.Courts.Count : 0))
            .ForMember(dest => dest.TotalTimeSlots, opt => opt.MapFrom(src => src.TimeSlots != null ? src.TimeSlots.Count : 0))
            .ForMember(dest => dest.GalleryImages, opt => opt.MapFrom(src => src.GalleryImages));

        CreateMap<CreateFacilityDto, Facility>()
            .ForMember(dest => dest.GalleryImagesJson, opt => opt.MapFrom(src => System.Text.Json.JsonSerializer.Serialize(src.GalleryImages, (System.Text.Json.JsonSerializerOptions?)null)));

        CreateMap<UpdateFacilityDto, Facility>()
            .ForMember(dest => dest.GalleryImagesJson, opt => opt.MapFrom(src => System.Text.Json.JsonSerializer.Serialize(src.GalleryImages, (System.Text.Json.JsonSerializerOptions?)null)));

        // Court Mappings
        CreateMap<Court, CourtDto>()
            .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility != null ? src.Facility.Name : string.Empty));
        CreateMap<SaveCourtDto, Court>();

        // TimeSlot Mappings
        CreateMap<TimeSlot, TimeSlotDto>()
            .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility != null ? src.Facility.Name : string.Empty));
        CreateMap<SaveTimeSlotDto, TimeSlot>();
        CreateMap<TimeSlot, AvailableTimeSlotDto>()
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.ExistingBookingId, opt => opt.Ignore());

        // Booking Mappings
        CreateMap<Booking, BookingDto>()
            .ForMember(dest => dest.PlayerName, opt => opt.MapFrom(src => src.Player != null ? src.Player.FullName : string.Empty))
            .ForMember(dest => dest.PlayerPhone, opt => opt.MapFrom(src => src.Player != null ? src.Player.PhoneNumber : string.Empty))
            .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.Court != null ? src.Court.FacilityId : 0))
            .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => (src.Court != null && src.Court.Facility != null) ? src.Court.Facility.Name : string.Empty))
            .ForMember(dest => dest.CourtName, opt => opt.MapFrom(src => src.Court != null ? src.Court.CourtName : string.Empty))
            .ForMember(dest => dest.SlotName, opt => opt.MapFrom(src => src.TimeSlot != null ? src.TimeSlot.SlotName : string.Empty))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.TimeSlot != null ? src.TimeSlot.StartTime : TimeSpan.Zero))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.TimeSlot != null ? src.TimeSlot.EndTime : TimeSpan.Zero));
        CreateMap<CreateBookingDto, Booking>();

        // Wallet Mappings
        CreateMap<Wallet, WalletDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

        CreateMap<WalletTransaction, WalletTransactionDto>();
    }
}
