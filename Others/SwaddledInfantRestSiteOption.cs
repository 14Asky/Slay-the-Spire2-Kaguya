using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    public sealed class NurtureRestSiteOption : RestSiteOption
    {
        private const int GoldCost = 50;
        private readonly RelicModel _sourceRelic;
        private bool _isEnabled;
        public override bool IsEnabled => _isEnabled;

        public override string OptionId => "NURTURE";

        public override LocString Description
        {
            get
            {
                var loc = new LocString("rest_site_ui", $"OPTION_{OptionId}.description");
                if (_isEnabled)
                {
                    loc.Add("Gold", GoldCost);
                    return loc;
                }
                return new LocString("rest_site_ui", $"OPTION_{OptionId}.descriptionDisabled");
            }
        }

        public NurtureRestSiteOption(Player owner, RelicModel sourceRelic) : base(owner)
        {
            _sourceRelic = sourceRelic;
            _isEnabled = owner.Gold >= GoldCost;
        }

        public override async Task<bool> OnSelect()
        {
            if (!_isEnabled) return false;

            // 扣除金币
            await PlayerCmd.LoseGold(GoldCost, Owner);

            // 随机获得一个遗物（从遗物池前方抽取）
            var newRelic = RelicFactory.PullNextRelicForRewardFromFront(Owner).ToMutable();
            await RelicCmd.Obtain(newRelic, Owner);

            // 替换遗物：移除当前遗物，添加“玩偶大小的女孩”
            await RelicCmd.Remove(_sourceRelic);
            var dollRelic = ModelDb.Relic<DollSizedGirl>().ToMutable();
            await RelicCmd.Obtain(dollRelic, Owner);

            return true;
        }

        public override Task DoLocalPostSelectVfx(CancellationToken ct = default)
        {
            NDebugAudioManager.Instance.Play("sts_sfx_coin.mp3", 1f, PitchVariance.Small);
            return Task.CompletedTask;
        }

        public override Task DoRemotePostSelectVfx()
        {
            NDebugAudioManager.Instance?.Play("sts_sfx_coin.mp3", 0.5f, PitchVariance.Small);
            NRestSiteCharacter character = NRestSiteRoom.Instance?.Characters.FirstOrDefault(c => c.Player == Owner);
            character?.Shake();
            NRelicFlashVfx flashVfx = NRelicFlashVfx.Create(ModelDb.Relic<DollSizedGirl>());
            if (flashVfx != null && character != null)
            {
                character.AddChildSafely(flashVfx);
                flashVfx.Position = Vector2.Zero;
            }
            return Task.CompletedTask;
        }
    }
}
