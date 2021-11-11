namespace CoachDraw.Rink
{
    public sealed class NhlRink : RinkSpecs
    {
        public override double RinkWidth { get; set; } = 200;
        public override double RinkHeight { get; set; } = 85;
        public override double BlueLineFromCenter { get; set; } = 25;
        public override double AuxDotsFromCenter { get; set; } = 20;
        public override double RedToBlue { get; set; } = 64;
        public override double EdgeToRed { get; set; } = 12;
        public override double CenterNetToCircle { get; set; } = 22;
        //public override double RedToCircle { get; set; } = 20;
        public override double EdgeToCircle { get; set; } = 32;
        public override double NetWidth { get; set; } = 4;
        public override double NetLength { get; set; } = 6;
        public override double NetArcRadius { get; set; } = 8;
        public override double CircleRadius { get; set; } = 15;
        public override double DotRadius { get; set; } = 1;
        public override double HashMarkSize { get; set; } = 4;
        public override bool GoalieTrapezoid { get; set; } = true;

        public NhlRink(double newScale) : base(newScale) { }
    }
}