namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// The language field in the string table file.
    /// </summary>
    public enum CsfLang
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        EnglishUS = 0,
        EnglishUK = 1,
        German = 2,
        French = 3,
        Spanish = 4,
        Italian = 5,
        Japanese = 6,
        Jabberwockie = 7,
        Korean = 8,
        Chinese = 9,
        Unknown = -1, // any value that is not from 0 to 9
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
