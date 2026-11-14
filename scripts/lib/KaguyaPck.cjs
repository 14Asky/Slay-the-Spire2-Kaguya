const fs = require('fs');
const crypto = require('crypto');

const hash = (algorithm, data) => crypto.createHash(algorithm).update(data).digest();
function safePath(value) {
  const name = value.replace(/^res:\/\//, '');
  if (!name || name.includes('\\') || name.startsWith('/') || name.includes('\0') ||
      name.split('/').some(part => !part || part === '.' || part === '..') || /^[A-Za-z]:/.test(name))
    throw Error('Unsafe PCK path: ' + value);
  return name;
}

function readPck(file) {
  const bytes = fs.readFileSync(file);
  if (bytes.length < 96 || bytes.readUInt32LE(0) !== 0x43504447) throw Error('Invalid PCK header');
  const version = bytes.readUInt32LE(4);
  if (version !== 2 && version !== 3) throw Error('Unsupported PCK version: ' + version);
  const base = Number(bytes.readBigUInt64LE(24));
  let offset = version === 3 ? Number(bytes.readBigUInt64LE(32)) : 96;
  const count = bytes.readUInt32LE(offset); offset += 4;
  const entries = new Map();
  for (let i = 0; i < count; i++) {
    const length = bytes.readUInt32LE(offset); offset += 4;
    const name = safePath(bytes.subarray(offset, offset + length).toString('utf8').replace(/\0+$/, '')); offset += length;
    const start = base + Number(bytes.readBigUInt64LE(offset)), size = Number(bytes.readBigUInt64LE(offset + 8));
    const md5 = bytes.subarray(offset + 16, offset + 32), flags = bytes.readUInt32LE(offset + 32); offset += 36;
    const data = bytes.subarray(start, start + size);
    if (entries.has(name) || flags || data.length !== size || !hash('md5', data).equals(md5))
      throw Error('Invalid PCK entry: ' + name);
    entries.set(name, data);
  }
  return entries;
}

// Multiple resource paths can point at the same immutable payload. Keep every
// path and checksum; only merge storage after an exact byte comparison.
function writePck(inventory, destination) {
  const header = Buffer.alloc(128);
  header.writeUInt32LE(0x43504447, 0);
  header.writeUInt32LE(3, 4);
  [4, 5, 1].forEach((value, i) => header.writeUInt32LE(value, 8 + i * 4));
  header.writeUInt32LE(2, 20); // PCK relative-file-base flag.
  header.writeBigUInt64LE(128n, 24);
  const fd = fs.openSync(destination, 'wx');
  const unique = new Map(), names = new Set(), records = [];
  let position = 128, logicalBytes = 0, uniqueBytes = 0;
  try {
    fs.writeSync(fd, header);
    for (const entry of inventory) {
      const name = safePath(entry.path);
      if (names.has(name)) throw Error('Duplicate resource path: ' + name);
      names.add(name);
      const data = fs.readFileSync(entry.source), key = hash('sha256', data).toString('hex');
      logicalBytes += data.length;
      let payload = unique.get(key);
      if (payload && !fs.readFileSync(payload.source).equals(data)) throw Error('Payload hash collision');
      if (!payload) {
        payload = { offset: position - 128, size: data.length, md5: hash('md5', data), source: entry.source };
        unique.set(key, payload);
        fs.writeSync(fd, data);
        const padding = (16 - (data.length % 16)) % 16;
        if (padding) fs.writeSync(fd, Buffer.alloc(padding));
        position += data.length + padding;
        uniqueBytes += data.length;
      }
      const encoded = Buffer.from(name, 'utf8'), padded = (encoded.length + 3) & ~3;
      const record = Buffer.alloc(4 + padded + 36);
      record.writeUInt32LE(padded, 0); encoded.copy(record, 4);
      record.writeBigUInt64LE(BigInt(payload.offset), 4 + padded);
      record.writeBigUInt64LE(BigInt(payload.size), 12 + padded);
      payload.md5.copy(record, 20 + padded);
      records.push(record);
    }
    header.writeBigUInt64LE(BigInt(position), 32);
    fs.writeSync(fd, header, 0, header.length, 0);
    const count = Buffer.alloc(4); count.writeUInt32LE(records.length);
    fs.writeSync(fd, count);
    for (const record of records) fs.writeSync(fd, record);
  } finally {
    fs.closeSync(fd);
  }
  return { entries: records.length, uniquePayloads: unique.size, logicalBytes, uniqueBytes,
    deduplicatedBytes: logicalBytes - uniqueBytes, fileBytes: fs.statSync(destination).size };
}

module.exports = { readPck, writePck, safePath };
