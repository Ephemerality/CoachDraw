namespace CoachDraw.Drawables.Items
{
    public sealed class Center : Item
    {
        public override TypeEnum Type => TypeEnum.Center;
        public Center(int? playerNumber = null) : base("C", playerNumber) { }
    }
}