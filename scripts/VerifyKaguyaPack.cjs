// Verify the distributed PCK itself, including raw Spine files and texture remaps.
// Usage: node scripts/VerifyKaguyaPack.cjs build/Kaguya.pck
const fs = require('fs');
const crypto = require('crypto');
const bytes = fs.readFileSync(process.argv[2]);
if (bytes.readUInt32LE(0) !== 0x43504447) throw Error('Invalid PCK header');
const base = Number(bytes.readBigUInt64LE(24));
let offset = bytes.readUInt32LE(4) === 3 ? Number(bytes.readBigUInt64LE(32)) : 96;
const count = bytes.readUInt32LE(offset); offset += 4;
const entries = new Map();
for (let i = 0; i < count; i++) {
  const length = bytes.readUInt32LE(offset); offset += 4;
  const name = bytes.subarray(offset, offset + length).toString().replace(/\0+$/, '').replace(/^res:\/\//, ''); offset += length;
  const start = base + Number(bytes.readBigUInt64LE(offset)), size = Number(bytes.readBigUInt64LE(offset + 8));
  const md5 = bytes.subarray(offset + 16, offset + 32).toString('hex'), flags = bytes.readUInt32LE(offset + 32); offset += 36;
  const data = bytes.subarray(start, start + size);
  if (entries.has(name) || flags || data.length !== size || crypto.createHash('md5').update(data).digest('hex') !== md5)
    throw Error('Invalid PCK entry: ' + name);
  if (/^(build|scripts|\.git|addons\/spine)\//.test(name) || /\.(dll|pdb|gdextension|docx)$/i.test(name))
    throw Error('Unexpected distribution file: ' + name);
  entries.set(name, data);
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
console.log(JSON.stringify({ success: true, entries: count, rawSpine: spine, texture, bgm }, null, 2));
