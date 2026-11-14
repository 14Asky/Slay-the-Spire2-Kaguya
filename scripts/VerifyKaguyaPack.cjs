// Verify the distributed PCK itself, including raw Spine files and texture remaps.
// Usage: node scripts/VerifyKaguyaPack.cjs build/Kaguya.pck
const fs = require('fs');
const crypto = require('crypto');
const { readPck } = require('./lib/KaguyaPck.cjs');
const entries = readPck(process.argv[2]);
const count = entries.size;
for (const name of entries.keys()) {
  if (/^(build|scripts|docs|\.git|addons\/spine)\//.test(name) || /\.(dll|pdb|gdextension|docx)$/i.test(name))
    throw Error('Unexpected distribution file: ' + name);
}
for (const asset of JSON.parse(fs.readFileSync(require('path').join(__dirname, 'unused-assets.json'), 'utf8')).assets) {
  for (const name of [asset.path, asset.path + '.import', ...asset.imported])
    if (entries.has(name)) throw Error('Retired asset was repackaged: ' + name);
}
const spine = {};
for (const ext of ['skel', 'atlas', 'png']) {
  const name = 'scenes/hinamods/spine/kaguya.' + ext, data = entries.get(name);
  if (!data || !data.equals(fs.readFileSync(name))) throw Error('Missing or outdated raw Spine asset: ' + name);
  spine[ext] = { bytes: data.length, sha256: crypto.createHash('sha256').update(data).digest('hex') };
}
function requireImported(name) {
  const remap = entries.get(name + '.import')?.toString().match(/path="res:\/\/([^\"]+)"/);
  if (!remap || !entries.get(remap[1])?.length) throw Error('Missing imported resource: ' + name);
  return { path: remap[1], bytes: entries.get(remap[1]).length };
}
const texture = requireImported('scenes/hinamods/spine/kaguya.png');
const bgm = Object.fromEntries(['HappySyn', 'LoveMyself', 'Remember'].map(n => [n, requireImported('debug_audio/' + n + '.ogg')]));
if (!entries.has('scenes/hinamods/HinaVisual.tscn')) throw Error('Missing legacy fallback scene');
const bossBgm = Object.fromEntries(['Moon', 'Shun', 'Reply'].map(n => [n, requireImported('debug_audio/' + n + '.mp3')]));
const backgroundPath = 'images/backgrounds/tsukito_bosatsu_original.ctex';
const background = entries.get(backgroundPath);
if (!background?.equals(fs.readFileSync(backgroundPath))) throw Error('Missing or outdated original Boss background');
for (const scene of [
  'scenes/backgrounds/final_act/final_act_background.tscn',
  'scenes/backgrounds/final_act/layers/final_act_bg_00.tscn'
]) {
  const data = entries.get(scene);
  if (!data?.equals(fs.readFileSync(scene)) || !data.toString().includes('res://' + backgroundPath))
    throw Error('Background scene does not use the packaged original texture: ' + scene);
}
const bossBackground = 'scenes/backgrounds/tsukito_bosatsu_boss_encounter/tsukito_bosatsu_boss_encounter_background.tscn';
const bossLayer = 'scenes/backgrounds/tsukito_bosatsu_boss_encounter/layers/tsukito_bosatsu_boss_encounter_bg_00.tscn';
for (const scene of [bossBackground, bossLayer]) {
  const data = entries.get(scene);
  if (!data?.equals(fs.readFileSync(scene)) || /Texture|glory|load_path/i.test(data.toString()))
    throw Error('Boss room must keep the requested plain black background: ' + scene);
}
if (!entries.get(bossLayer).toString().includes('color = Color(0, 0, 0, 1)'))
  throw Error('Boss background is not opaque black');
for (const lang of ['zhs', 'eng', 'jpn']) {
  const name = 'Kaguya/localization/' + lang + '/cards.json';
  const data = entries.get(name);
  if (!data?.equals(fs.readFileSync(name))) throw Error('Outdated card localization: ' + name);
  const loc = JSON.parse(data.toString().replace(/^\uFEFF/, ''));
  if (!loc['KAGUYA-JOY.title'] || !loc['KAGUYA-JOY.description'] || 'JOY.title' in loc || 'JOY.description' in loc)
    throw Error('Joy localization would collide with vanilla: ' + name);
}
console.log(JSON.stringify({ success: true, entries: count, rawSpine: spine, texture, bgm, bossBgm,
  bossBackground: { appearance: 'plain black', scene: bossBackground, layer: bossLayer },
  originalBackground: { path: backgroundPath, bytes: background.length, sha256: crypto.createHash('sha256').update(background).digest('hex') }
}, null, 2));
