namespace CoachDraw.Drawables.Items
{
    public sealed class Coach : Item
    {
        public override TypeEnum Type => TypeEnum.Coach;
        public override float FontSize => 16;
        public Coach(int? playerNumber = null) : base("©", playerNumber) { }
    }
}