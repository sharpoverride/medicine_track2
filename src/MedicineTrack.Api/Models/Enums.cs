namespace MedicineTrack.Api.Models;

public enum FrequencyType 
{ 
    DAILY, 
    WEEKLY, 
    MONTHLY, 
    AS_NEEDED, 
    EVERY_X_DAYS, 
    SPECIFIC_DAYS_OF_WEEK, 
    EVERY_X_WEEKS, 
    EVERY_X_MONTHS 
}

public enum LogStatus 
{ 
    TAKEN, 
    SKIPPED, 
    LOGGED_AS_NEEDED 
}

public enum InteractionSeverity 
{ 
    HIGH, 
    MODERATE, 
    LOW, 
    UNKNOWN 
}