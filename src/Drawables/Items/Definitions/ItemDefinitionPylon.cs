namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionPylon : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Pylon;
        public override string Name => Type.ToString();
        public override int Order => 110;
    }
}