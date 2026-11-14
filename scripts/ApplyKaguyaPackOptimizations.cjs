// Merge audited lossless compression results into a freshly prepared inventory.
// Usage: node scripts/ApplyKaguyaPackOptimizations.cjs <inventory> <texture-report> <audio-report> <output>
const fs = require('fs');
const crypto = require('crypto');
const [inventoryPath, texturePath, audioPath, outputPath] = process.argv.slice(2);
if (!outputPath) throw Error('Expected inventory, texture report, audio report and output paths');
const read = file => JSON.parse(fs.readFileSync(file, 'utf8'));
const digest = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const inventory = read(inventoryPath), textureReport = read(texturePath);
const textureReceipts = new Map(textureReport.receipts.map(r => [r.sourceSha256, r]));
const audio = new Map(read(audioPath).filter(r => r.useCompressed).map(r => [r.path, r]));
let textures = 0, streams = 0;
for (const entry of inventory) {
  const key = textureReport.paths[entry.path];
  const receipt = key ? textureReceipts.get(key) : audio.get(entry.path);
  if (!receipt) continue;
  const sourceHash = digest(entry.source);
  if (sourceHash !== receipt.sourceSha256 && sourceHash !== receipt.resultSha256)
    throw Error('Compression receipt is stale: ' + entry.path);
  if (digest(receipt.source) !== receipt.resultSha256)
    throw Error('Compressed resource changed after verification: ' + entry.path);
  entry.source = receipt.source;
  if (key) textures++; else streams++;
}
fs.writeFileSync(outputPath, JSON.stringify(inventory));
console.log(JSON.stringify({entries: inventory.length, textures, streams}));
