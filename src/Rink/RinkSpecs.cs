using System.ComponentModel;

namespace CoachDraw.Rink
{
    //public abstract double BlueLineFromCenter = 30
    //public double AuxDotsFromCenter = 22;
    //public double RedToBlue = 60;
    //public double EdgeToRed = 12;
    //public double CenterNetToCircle = 22;
    //public double RedToCircle = 20;
    //public double EdgeToCircle = 32;
    //public double NetWidth = 4;
    //public double NetLength = 6;
    //public double NetArcRadius = 8;
    //public double CircleRadius = 15;
    //public double DotRadius = 2.5;
    //public double HashMarkSize = 4;

    // http://www.nhl.com/nhl/en/v3/ext/rules/2017-2018-NHL-rulebook.pdf
    // http://www.iihf.com/fileadmin/user_upload/PDF/Sport/IIHF_Official_Rule_Book_2018.pdf
    public abstract class RinkSpecs
    {
        //Distances in feet
        public abstract double RinkWidth { get; set; }
        public abstract double RinkHeight { get; set; }
        public abstract double BlueLineFromCenter { get; set; }
        public abstract double AuxDotsFromCenter { get; set; }
        public abstract double RedToBlue { get; set; }
        public abstract double EdgeToRed { get; set; }
        public abstract double CenterNetToCircle { get; set; }
        //public abstract double RedToCircle { get; set; }
        public abstract double EdgeToCircle { get; set; }
        public abstract double NetWidth { get; set; }
        public abstract double NetLength { get; set; }
        public abstract double NetArcRadius { get; set; }
        public abstract double CircleRadius { get; set; }
        public abstract double DotRadius { get; set; }
        public abstract double HashMarkSize { get; set; }
        public abstract bool GoalieTrapezoid { get; set; }
        private double _curScale = 1;

        protected RinkSpecs(double newScale)
        {
            SetScale(newScale);
        }

        public void SetScale(double newScale)
        {
            RinkWidth /= _curScale; RinkWidth *= newScale;
            RinkHeight /= _curScale; RinkHeight *= newScale;
            BlueLineFromCenter /= _curScale; BlueLineFromCenter *= newScale;
            AuxDotsFromCenter /= _curScale; AuxDotsFromCenter *= newScale;
            RedToBlue /= _curScale; RedToBlue *= newScale;
            EdgeToRed /= _curScale; EdgeToRed *= newScale;
            CenterNetToCircle /= _curScale; CenterNetToCircle *= newScale;
            //RedToCircle /= _curScale; RedToCircle *= newScale;
            EdgeToCircle /= _curScale; EdgeToCircle *= newScale;
            NetWidth /= _curScale; NetWidth *= newScale;
            NetLength /= _curScale; NetLength *= newScale;
            NetArcRadius /= _curScale; NetArcRadius *= newScale;
            CircleRadius /= _curScale; CircleRadius *= newScale;
            DotRadius /= _curScale; DotRadius *= newScale;
            _curScale = newScale;
        }

        public static RinkSpecs GetRink(RinkType type, double scale)
        {
            return type switch
            {
                RinkType.IIHF => new IihfRink(scale),
                RinkType.NHL => new NhlRink(scale),
                _ => throw new InvalidEnumArgumentException()
            };
        }
    }
}