# 💊 Drug Catalog System

**Drug Catalog System** is a desktop application designed to automate home medicine inventory management, track expiration dates, and organize medication intake schedules.

The application is built using **C#** and **Windows Forms**, implements a multi-layer architecture, and supports multi-user environments.

---

## 🚀 Key Features

### 📦 Inventory Management
*   **Database Management:** Add, edit, and delete medications easily.
*   **Smart Search:** Quick filtering by name, active substance, or manufacturer.
*   **Expiration Control:** Visual color coding for expired drugs (Red) and those expiring soon (within 30 days).
*   **Categorization:** Automatic row highlighting based on the drug category (Analgesics, Antibiotics, etc.).

### ⏰ Smart Reminders
*   **Background Mode:** The application minimizes to the System Tray and runs in the background without interfering with your work.
*   **Auto-Deduction:** When you confirm medication intake via the notification, the system automatically deducts the dosage from the current stock.
*   **Spam Protection:** Notifications are triggered strictly on schedule, preventing duplicate alerts.

### 🔐 Security & User Management
*   **Role-Based Access Control (RBAC):** Admin (User Management) and Standard User roles.
*   **Data Isolation:** Complete clearing of RAM and data context when switching users prevents data leaks between sessions.
*   **Encryption:** User passwords are securely stored as SHA256 hashes.

### 📊 Analytics & Utilities
*   **Stock Calculator:** Forecasts the exact date when your medication will run out based on your daily dosage.
*   **Localization:** Dynamic language switching (English / Russian) without restarting the application. Both UI and reference data are translated instantly.

---

## 🛠 Tech Stack

*   **Language:** C#
*   **Platform:** .NET Framework (Windows Forms)
*   **Data Storage:** XML Serialization (No external SQL server required)
*   **Architecture:** Layered Architecture (Models, Services, Forms)

---

## 📂 Project Structure

The project follows Clean Code principles and Separation of Concerns:

```text
DrugCatalog_ver2/
├── 📁 Data/             # XML data files (generated automatically)
├── 📁 Forms/            # User Interface (UI) layers
│   ├── MainForm.cs      # Main Dashboard
│   ├── LoginForm.cs     # Authentication
│   └── ...
├── 📁 Models/           # Data Transfer Objects (DTO)
│   ├── Drug.cs          # Drug entity
│   ├── User.cs          # User entity
│   └── Locale.cs        # Localization dictionary
├── 📁 Services/         # Business Logic Layer
│   ├── XmlDataService.cs   # File system operations
│   ├── UserService.cs      # Hashing and Auth logic
│   └── ReminderService.cs  # Notification and Tray logic
└── Program.cs           # Application Entry Point
📥 Getting Started
Prerequisites: Windows 7/10/11, .NET Framework 4.7.2 or higher.
Installation:
Open the .sln solution file in Visual Studio.
Click Build Solution.
Press Start to run.
First Login:
A default administrator account is created on the first run:
Username: admin
Password: admin123
💡 Usage Guide
Add Medication: Press Ctrl+N or click "Add". Fill in the details (autocomplete is supported).
Set Reminder: Select a drug in the list and press Ctrl+Shift+R. Set the time and days of the week.
Minimize: Click the X button. The app will continue running in the background. Click the Tray Icon to restore it.
Calculator: Go to "Reminders" -> "Stock Calculator". Select a drug and enter your dosage to see how long it will last.
👨‍💻 Author
Coursework by student: Pavel Songin
Group: СДП-ПИ-241
University: Grodno State University named after Yanka Kupala
2025