namespace CoachDraw.Drawables.Items
{
    public sealed class DefensiveInt : Item
    {
        public override TypeEnum Type => TypeEnum.DefensiveInt;
        public override float FontSize => 16;
        public DefensiveInt(int? playerNumber = null) : base("Δ", playerNumber) { }
    }
}