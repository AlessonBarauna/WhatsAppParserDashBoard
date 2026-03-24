const fs = require('fs');
const qrcode = require('qrcode-terminal');
const { Client, LocalAuth } = require('whatsapp-web.js');
const axios = require('axios');
const crypto = require('crypto');
require('dotenv').config();

// ─── Config ──────────────────────────────────────────────────────────────────
const API_URL       = process.env.API_URL || 'http://localhost:5031/api/messages';
const LOG_IGNORED   = process.env.LOG_IGNORED === 'true';

// Groups to monitor — ALL messages from these groups are forwarded (no keyword filter)
const SUPPLIER_GROUPS = (process.env.SUPPLIER_GROUPS || '')
    .split(',')
    .map(g => g.trim().toLowerCase())
    .filter(Boolean);

// Keyword fallback for chats NOT in the monitored group list
const KEYWORDS = (process.env.KEYWORD_FILTER || 'IPHONE,XIAOMI,POCO,REDMI,SAMSUNG,MOTOROLA')
    .split(',')
    .map(k => k.trim().toUpperCase())
    .filter(Boolean);

const PROCESSED_FILE = 'processed_messages.json';

// ─── Dedup store ──────────────────────────────────────────────────────────────
let processed = new Set();
if (fs.existsSync(PROCESSED_FILE)) {
    try {
        processed = new Set(JSON.parse(fs.readFileSync(PROCESSED_FILE, 'utf8')));
    } catch {
        console.warn('[bot] Could not load processed messages file — starting fresh.');
    }
}

function saveProcessed() {
    fs.writeFileSync(PROCESSED_FILE, JSON.stringify([...processed]));
}

function messageHash(from, text) {
    return crypto.createHash('md5').update(`${from}_${text}`).digest('hex');
}

// ─── API ──────────────────────────────────────────────────────────────────────
async function sendToApi(payload, retries = 3) {
    for (let attempt = 1; attempt <= retries; attempt++) {
        try {
            const res = await axios.post(API_URL, payload, {
                headers: { 'Content-Type': 'application/json' },
                timeout: 10_000,
            });
            return res.data;
        } catch (err) {
            console.error(`[bot] API attempt ${attempt}/${retries} failed: ${err.message}`);
            if (attempt < retries) await sleep(1500 * attempt);
        }
    }
    throw new Error('API unreachable after all retries');
}

const sleep = ms => new Promise(r => setTimeout(r, ms));

// ─── WhatsApp client ──────────────────────────────────────────────────────────
const client = new Client({
    authStrategy: new LocalAuth(),
    puppeteer: {
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox'],
    },
});

client.on('qr', qr => {
    console.log('\n[bot] Scan the QR code to authenticate:\n');
    qrcode.generate(qr, { small: true });
});

client.on('ready', () => {
    console.log('\n[bot] ✓ WhatsApp connected and listening.');
    if (SUPPLIER_GROUPS.length) {
        console.log(`[bot] Monitoring groups: ${SUPPLIER_GROUPS.join(', ')}`);
    } else {
        console.log(`[bot] No groups configured — using keyword filter: ${KEYWORDS.join(', ')}`);
        console.log(`[bot] Tip: type "!grupos" in any chat to list available groups.`);
    }
});

client.on('auth_failure', () => {
    console.error('[bot] Authentication failed. Delete the .wwebjs_auth folder and restart.');
});

// ─── Message handler ──────────────────────────────────────────────────────────
client.on('message', async msg => {
    if (msg.type !== 'chat') return;

    const text = msg.body?.trim();
    if (!text) return;

    // ── Hidden command: list available groups ──────────────────────────────
    if (text === '!grupos') {
        const chats = await client.getChats();
        const groups = chats
            .filter(c => c.isGroup)
            .map(c => `• ${c.name}`)
            .join('\n');

        await msg.reply(groups
            ? `*Grupos disponíveis:*\n${groups}\n\nAdicione os nomes ao SUPPLIER_GROUPS no .env para monitorar.`
            : 'Nenhum grupo encontrado.');
        return;
    }

    const chat = await msg.getChat();
    const isGroup = chat.isGroup;
    const groupName = isGroup ? chat.name.toLowerCase() : null;

    // ── Decide whether to process this message ─────────────────────────────
    const isMonitoredGroup = isGroup && (
        SUPPLIER_GROUPS.length === 0
            ? false
            : SUPPLIER_GROUPS.some(g => groupName.includes(g))
    );

    const passesKeywordFilter = KEYWORDS.some(kw => text.toUpperCase().includes(kw));

    const shouldProcess = isMonitoredGroup || passesKeywordFilter;

    if (!shouldProcess) {
        if (LOG_IGNORED) console.log(`[bot] Ignored: "${text.substring(0, 60)}..."`);
        return;
    }

    // ── Dedup ──────────────────────────────────────────────────────────────
    const hash = messageHash(msg.from, text);
    if (processed.has(hash)) {
        console.log('[bot] Duplicate — skipping.');
        return;
    }

    const contact = await msg.getContact();
    const supplierName = contact.pushname || contact.name || groupName || 'Unknown';
    const source = isMonitoredGroup ? `group:${chat.name}` : 'keyword-match';

    console.log(`[bot] Processing from ${supplierName} (${source}): "${text.substring(0, 80)}"`);

    try {
        const result = await sendToApi({
            rawText: text,
            supplierName,
            supplierPhoneNumber: contact.number || null,
        });

        console.log(`[bot] ✓ Sent to API — ${JSON.stringify(result)}`);
        processed.add(hash);
        saveProcessed();
    } catch (err) {
        console.error(`[bot] ✗ Failed to send to API: ${err.message}`);
    }
});

client.initialize();
