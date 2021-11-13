namespace CoachDraw.Drawables.Items
{
    public sealed class Defenseman : Item
    {
        public override TypeEnum Type => TypeEnum.Defenseman;
        public Defenseman(int? playerNumber = null) : base("D", playerNumber) { }
    }
}