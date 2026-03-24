const fs = require('fs');
const qrcode = require('qrcode-terminal');
const { Client, LocalAuth } = require('whatsapp-web.js');
const axios = require('axios');
const crypto = require('crypto');
require('dotenv').config();

const API_URL = process.env.API_URL || 'http://localhost:5000/api/messages';
const PROCESSED_MESSAGES_FILE = 'processed_messages.json';

// Simple mechanism to avoid processing duplicates
let processedMessages = new Set();
if (fs.existsSync(PROCESSED_MESSAGES_FILE)) {
    const data = JSON.parse(fs.readFileSync(PROCESSED_MESSAGES_FILE, 'utf8'));
    processedMessages = new Set(data);
}

function saveProcessedMessages() {
    fs.writeFileSync(PROCESSED_MESSAGES_FILE, JSON.stringify(Array.from(processedMessages)));
}

const client = new Client({
    authStrategy: new LocalAuth(),
    puppeteer: {
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    }
});

client.on('qr', qr => {
    console.log('Scan the QR code below to authenticate WhatsApp:');
    qrcode.generate(qr, { small: true });
});

client.on('ready', () => {
    console.log('WhatsApp Bot is ready and listening for messages!');
});

async function sendToApiWithRetry(payload, maxRetries = 3) {
    for (let i = 0; i < maxRetries; i++) {
        try {
            const response = await axios.post(API_URL, payload, {
                headers: { 'Content-Type': 'application/json' }
            });
            return response.data;
        } catch (error) {
            console.error(`Attempt ${i + 1}/${maxRetries} to send message failed:`, error.message);
            if (i === maxRetries - 1) throw error;
            await new Promise(res => setTimeout(res, 2000 * (i + 1))); // exponential backoff
        }
    }
}

client.on('message', async msg => {
    // Only process text messages
    if (msg.type !== 'chat') return;

    const text = msg.body;
    const upperText = text.toUpperCase();

    // Filter by keywords
    if (upperText.includes('IPHONE') || upperText.includes('XIAOMI') || upperText.includes('POCO') || upperText.includes('REDMI')) {
        
        // Hash message to avoid duplicates based on exact content and sender within a short time
        const msgHash = crypto.createHash('md5').update(`${msg.from}_${text}`).digest('hex');
        
        if (processedMessages.has(msgHash)) {
            console.log('Duplicate message detected. Skipping.');
            return;
        }

        console.log(`Processing message from: ${msg.from}`);
        
        const contact = await msg.getContact();
        const payload = {
            rawText: text,
            supplierName: contact.pushname || contact.name || 'Unknown',
            supplierPhoneNumber: contact.number
        };

        try {
            const result = await sendToApiWithRetry(payload);
            console.log('Successfully sent to API:', result);
            
            // Mark as processed
            processedMessages.add(msgHash);
            saveProcessedMessages();
        } catch (error) {
            console.error('Failed to send message to API after retries. It will not be marked as processed.');
        }
    }
});

client.initialize();
