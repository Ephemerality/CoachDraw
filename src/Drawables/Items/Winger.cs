namespace CoachDraw.Drawables.Items
{
    public sealed class Winger : Item
    {
        public override TypeEnum Type => TypeEnum.Winger;
        public Winger(int? playerNumber = null) : base("W", playerNumber) { }
    }
}