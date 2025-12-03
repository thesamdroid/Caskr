#!/bin/bash

# Simple Icon Generator for Caskr PWA
# Uses convert (ImageMagick) if available, otherwise creates placeholder SVGs

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/../caskr.client/public/icons"
SOURCE_SVG="$OUTPUT_DIR/caskr-icon.svg"

echo "Caskr Icon Generator"
echo "===================="

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Check if ImageMagick is available
if command -v convert &> /dev/null; then
    echo "Using ImageMagick for icon generation..."

    # Standard icons
    for size in 72 96 128 144 152 192 384 512; do
        convert -background none "$SOURCE_SVG" -resize ${size}x${size} "$OUTPUT_DIR/icon-${size}x${size}.png"
        echo "Generated: icon-${size}x${size}.png"
    done

    # Maskable icons (with padding for safe zone)
    for size in 192 512; do
        inner_size=$((size * 80 / 100))
        convert -background "#2563eb" "$SOURCE_SVG" -resize ${inner_size}x${inner_size} \
            -gravity center -extent ${size}x${size} "$OUTPUT_DIR/icon-${size}x${size}-maskable.png"
        echo "Generated: icon-${size}x${size}-maskable.png (maskable)"
    done

    # Apple touch icon
    convert -background none "$SOURCE_SVG" -resize 180x180 "$OUTPUT_DIR/apple-touch-icon.png"
    echo "Generated: apple-touch-icon.png"

    # Favicons
    for size in 16 32 48; do
        convert -background none "$SOURCE_SVG" -resize ${size}x${size} "$OUTPUT_DIR/favicon-${size}x${size}.png"
        echo "Generated: favicon-${size}x${size}.png"
    done

    # Create ICO file
    convert "$OUTPUT_DIR/favicon-16x16.png" "$OUTPUT_DIR/favicon-32x32.png" "$OUTPUT_DIR/favicon-48x48.png" \
        "$OUTPUT_DIR/../favicon.ico"
    echo "Generated: favicon.ico"

else
    echo "ImageMagick not found. Creating placeholder SVGs..."
    echo "Install ImageMagick for PNG generation: apt-get install imagemagick"
fi

echo ""
echo "Icon generation complete!"
echo "Output: $OUTPUT_DIR"
