namespace MedicineTrack.Api.Models;

public record User(
    Guid Id, 
    string Email, 
    string Name, 
    string Timezone, 
    DateTimeOffset CreatedAt, 
    DateTimeOffset UpdatedAt
);