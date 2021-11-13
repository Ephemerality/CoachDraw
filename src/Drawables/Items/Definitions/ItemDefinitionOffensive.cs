namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionOffensive : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Offensive;
        public override string Name => Type.ToString();
        public override int Order => 10;
    }
}