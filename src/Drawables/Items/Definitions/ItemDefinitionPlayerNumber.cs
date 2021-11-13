namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionPlayerNumber : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.PlayerNumber;
        public override string Name => "Player Number";
        public override int Order => 90;
    }
}