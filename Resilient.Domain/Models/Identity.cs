namespace Resilient.Domain.Models
{
    internal static class Identity
    {
        public static string Generate()
        {
            return Nanoid.Nanoid.Generate();
        }
    }
}
