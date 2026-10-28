namespace FinderDemo.Models.Locations;

/// <summary>Kind of location ("Ablageort") a document folder can be stored in.</summary>
public enum LocationKind
{
    /// <summary>Process step ("Prozess-Schritt"). A folder can be in exactly one process step.</summary>
    ProcessStep,

    /// <summary>Archive category ("Archiv-Kategorie"). A folder can be in one or more archive categories.</summary>
    ArchiveCategory,
}
