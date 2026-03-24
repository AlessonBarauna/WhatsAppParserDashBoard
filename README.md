# WhatsApp Product Parsing SaaS MVP

A fully modular and scalable production-ready MVP that reads messages from specific WhatsApp groups (e.g. Apple/Xiaomi suppliers), extracts structured product data, stores it in PostgreSQL, and provides a dashboard with market insights and pricing.

---

## 🧱 Tech Stack
- **Backend**: .NET 8 (Clean Architecture)
- **Frontend**: Angular 17+ (Bootstrap 5)
- **Bot**: Node.js + `whatsapp-web.js` + Puppeteer (Headless)
- **Database**: PostgreSQL 15

---

## 🏃🏽‍♂️ How to Run Locally

You can run the entire system effortlessly via Docker Compose.

### Requirements:
- Docker Engine & Docker Compose
- Or alternatively, if running individually: Node 20+, .NET 8 SDK, Angular CLI, and PostgreSQL instance.

### Step-by-step Execution (Docker):
1. **Clone/Open the repository**.
2. Run the full stack:
   ```bash
   docker-compose up -d --build
   ```
3. **Wait a few moments** for the services to build and start.

### Generating Database Migrations:
If the database schema hasn't been created, from the `Backend` directory run:
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project Infrastructure/WhatsAppParser.Infrastructure.csproj --startup-project API/WhatsAppParser.API.csproj
```

### Authenticating the Bot:
1. Once the services are up, you need to scan the WhatsApp Web QR Code.
2. Run: 
   ```bash
   docker logs -f whatsapp_parser_bot
   ```
3. You will see a large QR code printed in the console. **Scan this QR Code with your cell phone (Linked Devices in WhatsApp)**.
4. The bot will print: `WhatsApp Bot is ready and listening for messages!`.

### Accessing the Dashboard:
1. Open your browser and go to: `http://localhost:4200`
2. You will see the Angular application running. 
3. *Note: Data will populate as the bot receives new messages.*

---

## 🔬 How to Test the Parser

1. After authenticating the bot.
2. From another phone or WhatsApp Web, send the following message to the phone number running the bot:

> 📱 IPHONE 13 PRO MAX 256Gb 🇺🇸
> 🟦 AZUL 💵R$2780

3. Look at your dashboard `http://localhost:4200/products` or `http://localhost:4200/dashboard` and verify the data has appeared!
   - You should see Brand: Apple, Model: iPhone 13 Pro Max, Storage: 256GB, Color: AZUL, Price: 2780.00, Condition: Battery100/USA.

---

## 🤔 Troubleshooting

- **Database Missing Tables:** If the .NET app starts but complains about tables, apply EF Core migrations via the terminal inside the `Backend` folder.
- **Bot not generating QR on Windows/Docker:** Check if Puppeteer installed its dependencies correctly. `whatsapp-web.js` needs Chromium/Chrome to render internally.
- **Duplicate Messages:** The Node.js bot uses a hashed memory registry (`processed_messages.json`) to skip identical prices and text blocks parsed from the same user to avoid flooding.
