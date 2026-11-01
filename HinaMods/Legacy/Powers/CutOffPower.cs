using Kaguya.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Kaguya.Powers
{
    public class LegacyCutOffPower : TemporaryStrengthPower
    {
        public override AbstractModel OriginModel => ModelDb.Card<CutOff>();
        protected override bool IsPositive => false;
    }
}