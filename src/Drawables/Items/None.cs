namespace CoachDraw.Drawables.Items
{
    public sealed class None : Item
    {
        public override TypeEnum Type => TypeEnum.None;
        public None(int? playerNumber = null) : base(null, playerNumber) { }
    }
}