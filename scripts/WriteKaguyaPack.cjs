// Usage: node scripts/WriteKaguyaPack.cjs <inventory.json> <new-output.pck>
const fs = require('fs');
const { readPck, writePck } = require('./lib/KaguyaPck.cjs');
if (process.argv.length !== 4) throw Error('Expected inventory and new PCK paths');
const inventory = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const result = writePck(inventory, process.argv[3]);
const verified = readPck(process.argv[3]);
for (const entry of inventory) {
  if (!verified.get(entry.path.replace(/^res:\/\//, '')).equals(fs.readFileSync(entry.source)))
    throw Error('Written resource differs from source: ' + entry.path);
}
console.log(JSON.stringify({success:true, ...result}));
