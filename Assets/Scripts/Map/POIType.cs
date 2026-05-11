public enum POIType
{
    Empty,      // пустой — идёшь, ничего не происходит
    Combat,     // бой
    Story,      // сюжетная сцена
    Event,      // случайное событие
    Merchant,   // торговец
    Boss,       // босс
    Friend      // встреча с другом
}

public enum POIState
{
    Hidden,     // не появился (шанс не выпал)
    Available,  // доступен
    Visited,    // посещён
    Locked      // недоступен (не сосед)
}