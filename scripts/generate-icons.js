#!/usr/bin/env node

/**
 * PWA Icon Generator Script for Caskr
 *
 * Generates all required icon sizes from source SVG.
 *
 * Requirements:
 *   npm install sharp
 *
 * Usage:
 *   node scripts/generate-icons.js
 *
 * This script generates:
 *   - Standard icons: 72, 96, 128, 144, 152, 192, 384, 512 px
 *   - Maskable icons: 192, 512 px (with 40% safe zone padding)
 *   - Apple touch icon: 180x180
 *   - Favicon sizes: 16, 32, 48 px
 *   - Shortcut icons: 96x96 for scan and tasks
 */

const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

// Configuration
const SOURCE_SVG = path.join(__dirname, '../caskr.client/public/icons/caskr-icon.svg');
const OUTPUT_DIR = path.join(__dirname, '../caskr.client/public/icons');

// Icon sizes configuration
const STANDARD_SIZES = [72, 96, 128, 144, 152, 192, 384, 512];
const MASKABLE_SIZES = [192, 512];
const FAVICON_SIZES = [16, 32, 48];

// Caskr brand colors
const BRAND_COLOR = '#2563eb';
const BG_COLOR = '#ffffff';

async function ensureDirectoryExists(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

async function generateIcon(size, outputPath, options = {}) {
  const { maskable = false, background = BG_COLOR } = options;

  try {
    let pipeline = sharp(SOURCE_SVG).resize(size, size);

    if (maskable) {
      // For maskable icons, add padding for 40% safe zone
      // Safe zone is 40% of icon size centered, so we need ~80% icon with 10% padding each side
      const iconSize = Math.floor(size * 0.8);
      const padding = Math.floor((size - iconSize) / 2);

      pipeline = sharp(SOURCE_SVG)
        .resize(iconSize, iconSize)
        .extend({
          top: padding,
          bottom: size - iconSize - padding,
          left: padding,
          right: size - iconSize - padding,
          background: BRAND_COLOR, // Maskable icons should have brand color background
        });
    }

    await pipeline.png().toFile(outputPath);
    console.log(`Generated: ${path.basename(outputPath)} (${size}x${size})`);
  } catch (error) {
    console.error(`Error generating ${outputPath}:`, error.message);
  }
}

async function generateFavicon(sizes, outputPath) {
  // Generate individual PNG files for each size first
  const pngPaths = [];

  for (const size of sizes) {
    const pngPath = path.join(OUTPUT_DIR, `favicon-${size}x${size}.png`);
    await generateIcon(size, pngPath);
    pngPaths.push(pngPath);
  }

  // Note: For proper ICO file generation, you'd need a library like 'png-to-ico'
  // For now, we generate individual PNG files and the 32x32 as the main favicon
  const favicon32Path = path.join(OUTPUT_DIR, 'favicon-32x32.png');
  if (fs.existsSync(favicon32Path)) {
    fs.copyFileSync(favicon32Path, path.join(OUTPUT_DIR, '../favicon.ico'));
    console.log('Generated: favicon.ico (copied from 32x32 PNG)');
  }
}

async function generateShortcutIcons() {
  // Generate scan shortcut icon (barcode scanner themed)
  const scanIconSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96">
    <rect width="96" height="96" fill="${BRAND_COLOR}" rx="16"/>
    <g fill="${BG_COLOR}">
      <!-- Barcode lines -->
      <rect x="20" y="25" width="4" height="46" rx="1"/>
      <rect x="28" y="25" width="8" height="46" rx="1"/>
      <rect x="40" y="25" width="4" height="46" rx="1"/>
      <rect x="48" y="25" width="12" height="46" rx="1"/>
      <rect x="64" y="25" width="4" height="46" rx="1"/>
      <rect x="72" y="25" width="8" height="46" rx="1"/>
      <!-- Scanner line -->
      <rect x="15" y="46" width="66" height="4" fill="#ef4444" rx="2"/>
    </g>
  </svg>`;

  // Generate tasks shortcut icon (checkbox themed)
  const tasksIconSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96">
    <rect width="96" height="96" fill="${BRAND_COLOR}" rx="16"/>
    <g fill="${BG_COLOR}">
      <!-- Checkbox -->
      <rect x="18" y="22" width="24" height="24" rx="4" fill="none" stroke="${BG_COLOR}" stroke-width="3"/>
      <path d="M24 34 L30 40 L40 28" fill="none" stroke="${BG_COLOR}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
      <!-- Lines -->
      <rect x="50" y="30" width="28" height="4" rx="2"/>
      <rect x="18" y="56" width="60" height="4" rx="2"/>
      <rect x="18" y="68" width="45" height="4" rx="2"/>
    </g>
  </svg>`;

  // Write SVGs and generate PNGs
  const scanSvgPath = path.join(OUTPUT_DIR, 'shortcut-scan.svg');
  const tasksSvgPath = path.join(OUTPUT_DIR, 'shortcut-tasks.svg');

  fs.writeFileSync(scanSvgPath, scanIconSvg);
  fs.writeFileSync(tasksSvgPath, tasksIconSvg);

  await sharp(Buffer.from(scanIconSvg)).resize(96, 96).png().toFile(path.join(OUTPUT_DIR, 'shortcut-scan.png'));
  await sharp(Buffer.from(tasksIconSvg)).resize(96, 96).png().toFile(path.join(OUTPUT_DIR, 'shortcut-tasks.png'));

  console.log('Generated: shortcut-scan.png (96x96)');
  console.log('Generated: shortcut-tasks.png (96x96)');
}

async function generateAppleTouchIcon() {
  await generateIcon(180, path.join(OUTPUT_DIR, 'apple-touch-icon.png'));
  console.log('Generated: apple-touch-icon.png (180x180)');
}

async function main() {
  console.log('Caskr PWA Icon Generator');
  console.log('========================\n');

  // Ensure output directory exists
  await ensureDirectoryExists(OUTPUT_DIR);

  // Check if source SVG exists
  if (!fs.existsSync(SOURCE_SVG)) {
    console.error(`Source SVG not found at: ${SOURCE_SVG}`);
    console.log('Please create the source SVG icon first.');
    process.exit(1);
  }

  console.log('Generating standard icons...');
  for (const size of STANDARD_SIZES) {
    await generateIcon(size, path.join(OUTPUT_DIR, `icon-${size}x${size}.png`));
  }

  console.log('\nGenerating maskable icons...');
  for (const size of MASKABLE_SIZES) {
    await generateIcon(size, path.join(OUTPUT_DIR, `icon-${size}x${size}-maskable.png`), { maskable: true });
  }

  console.log('\nGenerating favicon sizes...');
  await generateFavicon(FAVICON_SIZES, path.join(OUTPUT_DIR, '../favicon.ico'));

  console.log('\nGenerating Apple touch icon...');
  await generateAppleTouchIcon();

  console.log('\nGenerating shortcut icons...');
  await generateShortcutIcons();

  console.log('\n========================');
  console.log('Icon generation complete!');
  console.log(`Output directory: ${OUTPUT_DIR}`);
}

main().catch(console.error);
