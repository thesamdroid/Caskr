namespace Caskr.server.Models.Production;

/// <summary>
/// Types of production activities that can be scheduled.
/// </summary>
public enum ProductionType
{
    /// <summary>
    /// Grain mashing process.
    /// </summary>
    Mashing = 0,

    /// <summary>
    /// Fermentation of mash.
    /// </summary>
    Fermentation = 1,

    /// <summary>
    /// Distillation process.
    /// </summary>
    Distillation = 2,

    /// <summary>
    /// Filling spirits into barrels.
    /// </summary>
    Barreling = 3,

    /// <summary>
    /// Bottling finished product.
    /// </summary>
    Bottling = 4,

    /// <summary>
    /// Other production activities.
    /// </summary>
    Other = 5
}

/// <summary>
/// Status of a production run.
/// </summary>
public enum ProductionRunStatus
{
    /// <summary>
    /// Run is scheduled but not started.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Run is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Run has been completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Run was cancelled.
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Types of equipment used in production.
/// </summary>
public enum EquipmentType
{
    /// <summary>
    /// Distillation still.
    /// </summary>
    Still = 0,

    /// <summary>
    /// Fermentation vessel.
    /// </summary>
    Fermenter = 1,

    /// <summary>
    /// Mash tun for grain conversion.
    /// </summary>
    MashTun = 2,

    /// <summary>
    /// Bottling line equipment.
    /// </summary>
    BottlingLine = 3,

    /// <summary>
    /// Labeling machine.
    /// </summary>
    Labeler = 4,

    /// <summary>
    /// Storage or holding tank.
    /// </summary>
    Tank = 5,

    /// <summary>
    /// Other equipment types.
    /// </summary>
    Other = 6
}

/// <summary>
/// Status of an equipment booking.
/// </summary>
public enum EquipmentBookingStatus
{
    /// <summary>
    /// Booking is tentative and may change.
    /// </summary>
    Tentative = 0,

    /// <summary>
    /// Booking is confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Booking was cancelled.
    /// </summary>
    Cancelled = 2
}

/// <summary>
/// Types of calendar events.
/// </summary>
public enum CalendarEventType
{
    /// <summary>
    /// Production run event.
    /// </summary>
    ProductionRun = 0,

    /// <summary>
    /// Equipment maintenance event.
    /// </summary>
    Maintenance = 1,

    /// <summary>
    /// Meeting or planning session.
    /// </summary>
    Meeting = 2,

    /// <summary>
    /// Deadline or milestone.
    /// </summary>
    Deadline = 3,

    /// <summary>
    /// Other calendar events.
    /// </summary>
    Other = 4
}
