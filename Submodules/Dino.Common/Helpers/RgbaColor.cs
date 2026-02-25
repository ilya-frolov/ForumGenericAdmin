using System;

namespace Dino.Common.Helpers
{
    public class RgbaColor
    {
        private short _red = 0;
        private short _green = 0;
        private short _blue = 0;
        private float _alpha = 0;

        public RgbaColor(short red, short green, short blue, float alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public short Red
        {
            get
            {
                return _red;
            }
            set
            {
                ValidateColorValue(value);
                _red = value;
            }
        }

        public short Green
        {
            get
            {
                return _green;
            }
            set
            {
                ValidateColorValue(value);
                _green = value;
            }
        }

        public short Blue
        {
            get
            {
                return _blue;
            }
            set
            {
                ValidateColorValue(value);
                _blue = value;
            }
        }

        public float Alpha
        {
            get
            {
                return _alpha;
            }
            set
            {
                if ((value < 0) || (value > 1))
                {
                    throw new Exception("Invalid alpha value! Must be between 0 to 1.");
                }

                _alpha = value;
            }
        }

        private void ValidateColorValue(short colorValue)
        {
            if ((colorValue < 0) || (colorValue > 255))
            {
                throw new Exception("Invalid color value! Must be between 0 to 255.");
            }
        }

        public override string ToString()
        {
            return $"rgba({_red},{_green},{_blue},{_alpha})";
        }
    }
}
