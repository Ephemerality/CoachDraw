namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionCoach : ItemDefinition
    {
        public override Item.TypeEnum Type => Item.TypeEnum.Coach;
        public override string Name => Type.ToString();
        public override int Order => 70;
    }
}