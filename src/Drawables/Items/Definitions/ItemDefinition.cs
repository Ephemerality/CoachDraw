namespace CoachDraw.Drawables.Items.Definitions
{
    public abstract class ItemDefinition
    {
        public abstract Item.TypeEnum Type { get; }
        public abstract string Name { get; }
        public abstract int Order { get; }
    }
}