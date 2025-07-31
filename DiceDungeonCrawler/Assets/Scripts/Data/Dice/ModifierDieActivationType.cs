namespace Data.Dice
{
    public enum ModifierDieActivationType
    {
        INDIPENDENT = 0, //default
        ON_PLAYED = 1, //When dice are played
        ON_SCORED = 2, //When dice are scored
        ON_HELD = 3, //When dice are held
        ON_OTHER_MODIFIER_DIE = 4, //When other dice are activated
        PASSIVE = 5, //passive
    }
}