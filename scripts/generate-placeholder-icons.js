#!/usr/bin/env node

/**
 * Placeholder Icon Generator for Caskr PWA
 *
 * Creates minimal valid PNG files for development.
 * For production, use generate-icons.js with sharp installed.
 *
 * This script creates basic colored PNG files with proper dimensions.
 */

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const OUTPUT_DIR = path.join(__dirname, '../caskr.client/public/icons');
const SCREENSHOTS_DIR = path.join(__dirname, '../caskr.client/public/screenshots');
const SPLASH_DIR = path.join(__dirname, '../caskr.client/public/splash');

// Caskr brand color: #2563eb = RGB(37, 99, 235)
const BRAND_COLOR = { r: 37, g: 99, b: 235 };
const WHITE = { r: 255, g: 255, b: 255 };

// CRC32 lookup table
const crcTable = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    }
    table[n] = c;
  }
  return table;
})();

function crc32(data) {
  let crc = 0xffffffff;
  for (let i = 0; i < data.length; i++) {
    crc = crcTable[(crc ^ data[i]) & 0xff] ^ (crc >>> 8);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function createPNG(width, height, color) {
  // PNG signature
  const signature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

  // IHDR chunk
  const ihdrData = Buffer.alloc(13);
  ihdrData.writeUInt32BE(width, 0);
  ihdrData.writeUInt32BE(height, 4);
  ihdrData.writeUInt8(8, 8); // bit depth
  ihdrData.writeUInt8(2, 9); // color type (RGB)
  ihdrData.writeUInt8(0, 10); // compression method
  ihdrData.writeUInt8(0, 11); // filter method
  ihdrData.writeUInt8(0, 12); // interlace method

  const ihdr = createChunk('IHDR', ihdrData);

  // IDAT chunk (image data)
  // Create raw image data (filter byte + RGB for each row)
  const rowSize = 1 + width * 3;
  const rawData = Buffer.alloc(rowSize * height);

  for (let y = 0; y < height; y++) {
    const rowOffset = y * rowSize;
    rawData[rowOffset] = 0; // filter type: none

    for (let x = 0; x < width; x++) {
      const pixelOffset = rowOffset + 1 + x * 3;
      rawData[pixelOffset] = color.r;
      rawData[pixelOffset + 1] = color.g;
      rawData[pixelOffset + 2] = color.b;
    }
  }

  const compressedData = zlib.deflateSync(rawData, { level: 9 });
  const idat = createChunk('IDAT', compressedData);

  // IEND chunk
  const iend = createChunk('IEND', Buffer.alloc(0));

  return Buffer.concat([signature, ihdr, idat, iend]);
}

function createChunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length, 0);

  const typeBuffer = Buffer.from(type, 'ascii');
  const crcData = Buffer.concat([typeBuffer, data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(crcData), 0);

  return Buffer.concat([length, typeBuffer, data, crc]);
}

function ensureDir(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

function generateIcons() {
  console.log('Generating placeholder icons...\n');
  ensureDir(OUTPUT_DIR);

  // Standard icon sizes
  const standardSizes = [72, 96, 128, 144, 152, 192, 384, 512];
  for (const size of standardSizes) {
    const png = createPNG(size, size, BRAND_COLOR);
    fs.writeFileSync(path.join(OUTPUT_DIR, `icon-${size}x${size}.png`), png);
    console.log(`Generated: icon-${size}x${size}.png`);
  }

  // Maskable icons
  const maskableSizes = [192, 512];
  for (const size of maskableSizes) {
    const png = createPNG(size, size, BRAND_COLOR);
    fs.writeFileSync(path.join(OUTPUT_DIR, `icon-${size}x${size}-maskable.png`), png);
    console.log(`Generated: icon-${size}x${size}-maskable.png`);
  }

  // Apple touch icon
  const applePng = createPNG(180, 180, BRAND_COLOR);
  fs.writeFileSync(path.join(OUTPUT_DIR, 'apple-touch-icon.png'), applePng);
  console.log('Generated: apple-touch-icon.png');

  // Favicon sizes
  const faviconSizes = [16, 32, 48];
  for (const size of faviconSizes) {
    const png = createPNG(size, size, BRAND_COLOR);
    fs.writeFileSync(path.join(OUTPUT_DIR, `favicon-${size}x${size}.png`), png);
    console.log(`Generated: favicon-${size}x${size}.png`);
  }

  // Shortcut icons
  const shortcutPng = createPNG(96, 96, BRAND_COLOR);
  fs.writeFileSync(path.join(OUTPUT_DIR, 'shortcut-scan.png'), shortcutPng);
  fs.writeFileSync(path.join(OUTPUT_DIR, 'shortcut-tasks.png'), shortcutPng);
  console.log('Generated: shortcut-scan.png');
  console.log('Generated: shortcut-tasks.png');
}

function generateScreenshots() {
  console.log('\nGenerating placeholder screenshots...\n');
  ensureDir(SCREENSHOTS_DIR);

  const screenshots = [
    { name: 'dashboard-mobile.png', width: 1080, height: 1920 },
    { name: 'barrels-mobile.png', width: 1080, height: 1920 },
    { name: 'tasks-mobile.png', width: 1080, height: 1920 },
  ];

  for (const { name, width, height } of screenshots) {
    const png = createPNG(width, height, BRAND_COLOR);
    fs.writeFileSync(path.join(SCREENSHOTS_DIR, name), png);
    console.log(`Generated: ${name} (${width}x${height})`);
  }
}

function generateSplashScreens() {
  console.log('\nGenerating placeholder iOS splash screens...\n');
  ensureDir(SPLASH_DIR);

  // iOS device splash screen sizes
  const splashScreens = [
    // iPhone
    { name: 'apple-splash-2048-2732.png', width: 2048, height: 2732 }, // 12.9" iPad Pro
    { name: 'apple-splash-1668-2388.png', width: 1668, height: 2388 }, // 11" iPad Pro
    { name: 'apple-splash-1536-2048.png', width: 1536, height: 2048 }, // 10.2" iPad
    { name: 'apple-splash-1668-2224.png', width: 1668, height: 2224 }, // 10.5" iPad Pro
    { name: 'apple-splash-1620-2160.png', width: 1620, height: 2160 }, // 10.2" iPad
    { name: 'apple-splash-1284-2778.png', width: 1284, height: 2778 }, // iPhone 12 Pro Max
    { name: 'apple-splash-1170-2532.png', width: 1170, height: 2532 }, // iPhone 12 Pro
    { name: 'apple-splash-1125-2436.png', width: 1125, height: 2436 }, // iPhone X/XS/11 Pro
    { name: 'apple-splash-1242-2688.png', width: 1242, height: 2688 }, // iPhone XS Max/11 Pro Max
    { name: 'apple-splash-828-1792.png', width: 828, height: 1792 }, // iPhone XR/11
    { name: 'apple-splash-1242-2208.png', width: 1242, height: 2208 }, // iPhone 6+/7+/8+
    { name: 'apple-splash-750-1334.png', width: 750, height: 1334 }, // iPhone 6/7/8
    { name: 'apple-splash-640-1136.png', width: 640, height: 1136 }, // iPhone 5/SE
    // Landscape versions
    { name: 'apple-splash-2732-2048.png', width: 2732, height: 2048 },
    { name: 'apple-splash-2388-1668.png', width: 2388, height: 1668 },
    { name: 'apple-splash-2048-1536.png', width: 2048, height: 1536 },
    { name: 'apple-splash-2224-1668.png', width: 2224, height: 1668 },
    { name: 'apple-splash-2160-1620.png', width: 2160, height: 1620 },
    { name: 'apple-splash-2778-1284.png', width: 2778, height: 1284 },
    { name: 'apple-splash-2532-1170.png', width: 2532, height: 1170 },
    { name: 'apple-splash-2436-1125.png', width: 2436, height: 1125 },
    { name: 'apple-splash-2688-1242.png', width: 2688, height: 1242 },
    { name: 'apple-splash-1792-828.png', width: 1792, height: 828 },
    { name: 'apple-splash-2208-1242.png', width: 2208, height: 1242 },
    { name: 'apple-splash-1334-750.png', width: 1334, height: 750 },
    { name: 'apple-splash-1136-640.png', width: 1136, height: 640 },
  ];

  for (const { name, width, height } of splashScreens) {
    const png = createPNG(width, height, BRAND_COLOR);
    fs.writeFileSync(path.join(SPLASH_DIR, name), png);
    console.log(`Generated: ${name}`);
  }
}

console.log('Caskr PWA Asset Generator');
console.log('=========================\n');

generateIcons();
generateScreenshots();
generateSplashScreens();

console.log('\n=========================');
console.log('Asset generation complete!');
console.log('\nNOTE: These are solid color placeholders.');
console.log('For production, replace with proper branded assets.');
