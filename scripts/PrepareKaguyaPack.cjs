const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const root = path.resolve(__dirname, '../build');
const bytes = fs.readFileSync(process.argv[2] || path.join(root, 'Kaguya-v3.2.1-candidate.pck'));
if (bytes.readUInt32LE(0) !== 0x43504447) throw Error('Invalid PCK');
const version = bytes.readUInt32LE(4), base = Number(bytes.readBigUInt64LE(24));
let offset = version === 3 ? Number(bytes.readBigUInt64LE(32)) : 96;
const count = bytes.readUInt32LE(offset); offset += 4;
const staging = fs.mkdtempSync(path.join(root, 'pack-staging-'));
const inventory = [];
for (let i = 0; i < count; i++) {
  const size = bytes.readUInt32LE(offset); offset += 4;
  const name = bytes.subarray(offset, offset + size).toString().replace(/\0+$/, '').replace(/^res:\/\//, ''); offset += size;
  const start = base + Number(bytes.readBigUInt64LE(offset)), length = Number(bytes.readBigUInt64LE(offset + 8));
  const md5 = bytes.subarray(offset + 16, offset + 32).toString('hex'), flags = bytes.readUInt32LE(offset + 32); offset += 36;
  const destination = path.resolve(staging, name);
  if (!destination.startsWith(path.resolve(staging) + path.sep) || flags !== 0) throw Error('Unsafe PCK entry: ' + name);
  const data = bytes.subarray(start, start + length);
  if (crypto.createHash('md5').update(data).digest('hex') !== md5) throw Error('PCK checksum failed: ' + name);
  fs.mkdirSync(path.dirname(destination), {recursive: true}); fs.writeFileSync(destination, data);
  inventory.push({path: 'res://' + name, source: destination});
}
// Spine's importer writes zero-byte marker resources. Its runtime loader also needs
// the original skeleton and atlas, which Godot's normal export omitted.
for (const name of ['scenes/hinamods/spine/kaguya.skel', 'scenes/hinamods/spine/kaguya.atlas']) {
  if (inventory.some(e => e.path === 'res://' + name)) throw Error('Unexpected duplicate raw Spine asset');
  inventory.push({path: 'res://' + name, source: path.join(root, '..', name)});
}
fs.writeFileSync(path.join(root, 'pack-v321-inventory.json'), JSON.stringify(inventory));
console.log(JSON.stringify({validatedEntries: count, addedRawSpineAssets: 2, entriesToPack: inventory.length}));
