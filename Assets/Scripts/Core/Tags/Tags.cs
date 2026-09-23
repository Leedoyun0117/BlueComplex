namespace BlueComplex.Core.Tags
{
    public enum TimeTag
    {
        None,
        Past,
        Present,
        Future
    }

    public enum PersonTag
    {
        None,
        Family,
        Other,
        Friend,
        Lover
    }

    public enum EmotionTag
    {
        Sadness,
        Disgust,
        Fear,
        Happiness,
        Love,
        Anger
    }

    /// <summary>침체(-1) / 흥분(+1). 인디케이터 이동량 계산의 기준.</summary>
    public enum Polarity
    {
        Depressed = -1,
        Excited = 1
    }
}
