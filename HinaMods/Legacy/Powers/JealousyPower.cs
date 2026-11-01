using Kaguya.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Kaguya.Powers
{
    public class LegacyJealousyPower : TemporaryStrengthPower
    {
        public override AbstractModel OriginModel => ModelDb.Card<Jealousy>();
    }
}