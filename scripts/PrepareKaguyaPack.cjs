const fs = require('fs');
const path = require('path');
const { readPck } = require('./lib/KaguyaPck.cjs');
const root = path.resolve(__dirname, '../build');
const input = process.argv[2];
if (!input) throw Error('Usage: node scripts/PrepareKaguyaPack.cjs <exported-candidate.pck>');
const packed = readPck(input);
const count = packed.size;
const staging = fs.mkdtempSync(path.join(root, 'pack-staging-'));
const inventory = [];
const retired = JSON.parse(fs.readFileSync(path.join(__dirname, 'unused-assets.json'), 'utf8')).assets;
const excluded = new Set(retired.flatMap(a => [a.path, a.path + '.import', ...a.imported]));
for (const [name, data] of packed) {
  if (excluded.has(name)) continue;
  const destination = path.resolve(staging, name);
  if (!destination.startsWith(path.resolve(staging) + path.sep)) throw Error('Unsafe PCK entry: ' + name);
  fs.mkdirSync(path.dirname(destination), {recursive: true}); fs.writeFileSync(destination, data);
  inventory.push({path: 'res://' + name, source: destination});
}
// Keep raw Spine data and migration resources in sync even when reusing a prior
// export. The original Boss background must not depend on the game's import cache.
const sourceOverrides = [
  'scenes/hinamods/spine/kaguya.skel', 'scenes/hinamods/spine/kaguya.atlas', 'scenes/hinamods/spine/kaguya.png',
  'images/backgrounds/tsukito_bosatsu_original.ctex',
  'scenes/backgrounds/tsukito_bosatsu_boss_encounter/tsukito_bosatsu_boss_encounter_background.tscn',
  'scenes/backgrounds/tsukito_bosatsu_boss_encounter/layers/tsukito_bosatsu_boss_encounter_bg_00.tscn',
  'scenes/backgrounds/final_act/final_act_background.tscn',
  'scenes/backgrounds/final_act/layers/final_act_bg_00.tscn',
  ...['zhs', 'eng', 'jpn'].map(lang => 'Kaguya/localization/' + lang + '/cards.json')
];
for (const name of sourceOverrides) {
  const existing = inventory.findIndex(e => e.path === 'res://' + name);
  if (existing !== -1) inventory.splice(existing, 1);
  inventory.push({path: 'res://' + name, source: path.join(root, '..', name)});
}
fs.writeFileSync(path.join(root, 'pack-inventory.json'), JSON.stringify(inventory));
console.log(JSON.stringify({validatedEntries: count, retiredAssets: retired.length, rawSpineAssets: 3, sourceOverrides: sourceOverrides.length, entriesToPack: inventory.length}));
