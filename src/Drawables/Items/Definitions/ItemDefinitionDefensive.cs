namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionDefensive : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Defensive;
        public override string Name => Type.ToString();
        public override int Order => 20;
    }
}