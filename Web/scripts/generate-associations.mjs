import { mkdirSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const publicPath = fileURLToPath(new URL('../public/', import.meta.url))
const fingerprint = process.env.ANDROID_CERT_SHA256 ?? ''
const team = process.env.APPLE_TEAM_ID ?? ''
const publisher = process.env.ADMOB_PUBLISHER_ID ?? ''
const validFingerprint = /^(?:[A-Fa-f0-9]{2}:){31}[A-Fa-f0-9]{2}$/.test(fingerprint)
const validTeam = /^[A-Z0-9]{10}$/.test(team)
const validPublisher = /^pub-\d{16}$/.test(publisher)
if (process.env.MONETIZATION_RELEASE === 'true' && (!validFingerprint || !validTeam || !validPublisher))
  throw new Error('Configure ANDROID_CERT_SHA256, APPLE_TEAM_ID and ADMOB_PUBLISHER_ID before a monetization release.')
mkdirSync(`${publicPath}/.well-known`, { recursive: true })
writeFileSync(`${publicPath}/.well-known/assetlinks.json`, JSON.stringify(validFingerprint ? [{ relation: ['delegate_permission/common.handle_all_urls'], target: { namespace: 'android_app', package_name: 'com.palabravo.app', sha256_cert_fingerprints: [fingerprint] } }] : []))
writeFileSync(`${publicPath}/.well-known/apple-app-site-association`, JSON.stringify({ applinks: { details: validTeam ? [{ appIDs: [`${team}.com.palabravo.app`], components: [{ '/': '/reto/*' }, { '/': '/challenge/*' }, { '/': '/semanal' }] }] : [] } }))
writeFileSync(`${publicPath}/app-ads.txt`, validPublisher ? `google.com, ${publisher}, DIRECT, f08c47fec0942fa0\n` : '')
