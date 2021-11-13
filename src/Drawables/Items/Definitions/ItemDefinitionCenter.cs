namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionCenter : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Center;
        public override string Name => Type.ToString();
        public override int Order => 60;
    }
}