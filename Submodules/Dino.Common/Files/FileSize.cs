namespace Dino.Common.Files
{
    public class FileSize
    {
        private readonly double _bytes;

        public FileSize(double bytes)
        {
            _bytes = bytes;
        }

        public static FileSize FromBytes(double bytes)
        {
            return new FileSize(bytes);
        }

        public static FileSize FromKilobytes(double kilobytes)
        {
            return new FileSize(kilobytes * 1024);
        }

        public static FileSize FromMegabytes(double megabytes)
        {
            return FromKilobytes(megabytes * 1024);
        }

        public static FileSize FromGigabytes(double gigabytes)
        {
            return FromMegabytes(gigabytes * 1024);
        }

        public static FileSize FromTerabytes(double terabytes)
        {
            return FromGigabytes(terabytes * 1024);
        }

        public static FileSize FromPetabytes(double petabytes)
        {
            return FromTerabytes(petabytes * 1024);
        }

        public double ToBytes()
        {
            return _bytes;
        }

        public double ToKilobytes()
        {
            return (_bytes/1024);
        }

        public double ToMegabytes()
        {
            return (ToKilobytes()/1024);
        }

        public double ToGigabytes()
        {
            return (ToMegabytes()/1024);
        }

        public double ToTerabytes()
        {
            return (ToGigabytes()/1024);
        }

        public double ToPetabytes()
        {
            return (ToTerabytes()/1024);
        }
    }
}
