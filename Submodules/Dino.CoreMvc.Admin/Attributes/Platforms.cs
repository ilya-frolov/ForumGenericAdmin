namespace Dino.CoreMvc.Admin.Attributes
{
    [Flags]
    public enum Platforms
    {
        Desktop = 1,
        Tablet = 2,
        Mobile = 4,
        App = 8,
        Custom1 = 16,
        Custom2 = 32,
        Custom3 = 64
    }
}
