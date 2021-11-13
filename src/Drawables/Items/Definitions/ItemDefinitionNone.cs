namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionNone : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.None;
        public override string Name => Type.ToString();
        public override int Order => 100;
    }
}