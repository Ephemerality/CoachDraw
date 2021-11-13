namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionWinger : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Winger;
        public override string Name => Type.ToString();
        public override int Order => 50;
    }
}