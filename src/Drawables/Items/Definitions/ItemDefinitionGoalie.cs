namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionGoalie : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Goalie;
        public override string Name => Type.ToString();
        public override int Order => 80;
    }
}