using System;
using System.Linq;
using BlueComplex.Core.Tags;
using BlueComplex.Core.Traits;

namespace BlueComplex.Core.Stability
{
    public interface IEmotionEvaluator
    {
        int Evaluate(TagSet finalTags);
    }
}
