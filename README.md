<div align="center">
  <img src="https://img.shields.io/badge/Microsoft%20Dynamics%20365-00a1f1?style=for-the-badge&logo=microsoft-dynamics-365&logoColor=white" />
  <img src="https://img.shields.io/badge/Power%20Platform-742774?style=for-the-badge&logo=microsoft-power-platform&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img src="https://img.shields.io/badge/JavaScript-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black" />
</div>

<h1 align="center">🏥 Apollo Hospital Management Platform</h1>

<p align="center">
  <strong>An enterprise-grade D365 CE & Power Platform solution for modern healthcare orchestration.</strong>
  <br />
  <i>Managing the end-to-end patient lifecycle from WhatsApp engagement to automated clinical billing.</i>
</p>

<div align="center">
  <img src="YOUR_MAIN_BANNER_LINK_HERE" alt="Project Banner" width="800" />
</div>

---

### 📋 Project Overview
The Apollo Hospital Platform is a custom-engineered solution built on **Dynamics 365 CE** and **Dataverse**. It solves the fragmentation between medical consultations, lab testing, and financial departments by automating data flow via **Plugins**, **Custom Actions**, and **Power Automate**.

---

### 🛠️ Technical Architecture

<table>
  <tr>
    <td width="50%">
      <h4>🔹 Pro-Code Development</h4>
      <ul>
        <li><b>C# Plugins:</b> Real-time logic for complex medical billing and invoice line generation.</li>
        <li><b>JavaScript:</b> Client-side form scripting, dynamic field behaviors, and Web Resource integration.</li>
        <li><b>Custom Actions:</b> Server-side logic units called via JS and Power Automate.</li>
        <li><b>WhatsApp API:</b> Integrated via custom bridge for automated patient alerts.</li>
      </ul>
    </td>
    <td width="50%">
      <h4>🔹 Low-Code / Configuration</h4>
      <ul>
        <li><b>Model-Driven Apps:</b> Role-based interfaces for Doctors and Admin staff.</li>
        <li><b>Power Automate:</b> Asynchronous flows for email templates and report scheduling.</li>
        <li><b>Security Architecture:</b> Complex matrix using Security Roles, Teams, and Business Units.</li>
        <li><b>Business Process Flows:</b> Guided stages for "Admission → Lab → Billing → Discharge".</li>
      </ul>
    </td>
  </tr>
</table>

---

### ✨ Advanced Features Implemented

#### 🧬 Dataverse Modeling & Logic
* **Advanced Relationships:** Expert implementation of 1:N, N:N, and **Polymorphic Lookups** to link patients to varying medical entities.
* **Logic Layers:** Hybrid use of **Legacy Workflows** (for instant execution) and **Modern Power Automate** (for external integrations).
* **UI/UX Extensions:** Customized the **Command Bar (Ribbon)** and developed **Custom Pages** for a modern healthcare dashboard.

#### 💬 Communication & Integration
* **WhatsApp Integration:** Designed a system to trigger WhatsApp notifications (Appointment reminders/Results) based on status changes in Dataverse.
* **Templates:** Standardized Email and Document templates for professional medical reporting.

---

### 📸 System Gallery
<div align="center">
  <table border="0">
    <tr>
      <td><img src="IMAGE_LINK_1" width="250" /><br /><sub>Patient Dashboard</sub></td>
      <td><img src="IMAGE_LINK_2" width="250" /><br /><sub>Billing Logic</sub></td>
      <td><img src="IMAGE_LINK_3" width="250" /><br /><sub>Custom Process Flow</sub></td>
    </tr>
  </table>
</div>

---

### 📂 Repository Structure
```bash
├── 📦 Solutions             # Managed/Unmanaged Exported Solutions
├── 📜 WebResources         # JavaScript Logic, HTML Forms, CSS
├── ⚙️ Plugins              # C# Source Code & Plugin Registration steps
├── 🤖 Workflows            # Exported Power Automate & Classic Flows
└── 📖 README.md
