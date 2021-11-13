namespace CoachDraw.Drawables.Items
{
    public sealed class Offensive : Item
    {
        public override TypeEnum Type => TypeEnum.Offensive;
        public Offensive(int? playerNumber = null) : base("O", playerNumber) { }
    }
}