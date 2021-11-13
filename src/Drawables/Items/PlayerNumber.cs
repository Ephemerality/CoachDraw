namespace CoachDraw.Drawables.Items
{
    public sealed class PlayerNumber : Item
    {
        public override TypeEnum Type => TypeEnum.PlayerNumber;
        public PlayerNumber(int playerNumber) : base(null, playerNumber) { }
    }
}