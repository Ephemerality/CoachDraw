namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionPuck : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Puck;
        public override string Name => Type.ToString();
        public override int Order => 120;
    }
}