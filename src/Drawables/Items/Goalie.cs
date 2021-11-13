namespace CoachDraw.Drawables.Items
{
    public sealed class Goalie : Item
    {
        public override TypeEnum Type => TypeEnum.Goalie;
        public Goalie(int? playerNumber = null) : base("G", playerNumber) { }
    }
}