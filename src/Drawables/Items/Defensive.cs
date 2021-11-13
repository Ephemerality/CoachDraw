namespace CoachDraw.Drawables.Items
{
    public sealed class Defensive : Item
    {
        public override TypeEnum Type => TypeEnum.Defensive;
        public Defensive(int? playerNumber = null) : base("X", playerNumber) { }
    }
}