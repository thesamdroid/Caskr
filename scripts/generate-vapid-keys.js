#!/usr/bin/env node

/**
 * VAPID Key Generator for Caskr Push Notifications
 *
 * Generates VAPID (Voluntary Application Server Identification) keys
 * required for Web Push notifications.
 *
 * Requirements:
 *   npm install web-push
 *
 * Usage:
 *   node scripts/generate-vapid-keys.js
 *
 * Output:
 *   Generates publicKey and privateKey for use in:
 *   - Backend appsettings.json (VapidPublicKey, VapidPrivateKey)
 *   - Frontend environment config
 */

const webpush = require('web-push');
const fs = require('fs');
const path = require('path');

function generateKeys() {
  console.log('Caskr VAPID Key Generator');
  console.log('=========================\n');

  const vapidKeys = webpush.generateVAPIDKeys();

  console.log('Generated VAPID Keys:\n');
  console.log('Public Key:');
  console.log(vapidKeys.publicKey);
  console.log('\nPrivate Key:');
  console.log(vapidKeys.privateKey);

  console.log('\n=========================\n');
  console.log('Add these to your configuration:\n');

  console.log('1. Backend - appsettings.json:');
  console.log(`{
  "PushNotifications": {
    "VapidSubject": "mailto:admin@caskr.co",
    "VapidPublicKey": "${vapidKeys.publicKey}",
    "VapidPrivateKey": "${vapidKeys.privateKey}"
  }
}`);

  console.log('\n2. Frontend - .env:');
  console.log(`VITE_VAPID_PUBLIC_KEY=${vapidKeys.publicKey}`);

  // Optionally save to a file
  const outputPath = path.join(__dirname, 'vapid-keys.json');
  const keysContent = {
    subject: 'mailto:admin@caskr.co',
    publicKey: vapidKeys.publicKey,
    privateKey: vapidKeys.privateKey,
    generatedAt: new Date().toISOString(),
    note: 'Keep privateKey secure! Never commit to version control.',
  };

  fs.writeFileSync(outputPath, JSON.stringify(keysContent, null, 2));
  console.log(`\nKeys saved to: ${outputPath}`);
  console.log('WARNING: Add vapid-keys.json to .gitignore to prevent committing secrets!');
}

// Check if web-push is available
try {
  require.resolve('web-push');
  generateKeys();
} catch (e) {
  console.log('web-push module not found.');
  console.log('Install it with: npm install web-push');
  console.log('\nGenerating placeholder keys for development...\n');

  // Generate placeholder development keys
  const crypto = require('crypto');

  // Generate a base64url encoded random string (simulating VAPID keys)
  const generateBase64Url = (length) => {
    return crypto
      .randomBytes(length)
      .toString('base64')
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  };

  // Note: These are NOT valid VAPID keys, just placeholders
  console.log('PLACEHOLDER KEYS (install web-push for real keys):');
  console.log(`Public Key: ${generateBase64Url(65)}`);
  console.log(`Private Key: ${generateBase64Url(32)}`);
  console.log('\nThese are NOT valid for production use!');
}
