namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionPucks : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Pucks;
        public override string Name => Type.ToString();
        public override int Order => 130;
    }
}