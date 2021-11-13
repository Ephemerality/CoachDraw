namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionDefenseman : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Defenseman;
        public override string Name => Type.ToString();
        public override int Order => 30;
    }
}