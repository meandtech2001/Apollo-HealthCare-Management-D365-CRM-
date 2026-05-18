<div align="center">

<img src="https://img.shields.io/badge/Microsoft%20Dynamics%20365-00a1f1?style=for-the-badge&logo=microsoft&logoColor=white" />
<img src="https://img.shields.io/badge/Power%20Platform-742774?style=for-the-badge&logo=microsoft&logoColor=white" />
<img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
<img src="https://img.shields.io/badge/JavaScript-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black" />
<img src="https://img.shields.io/badge/Azure-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white" />
<img src="https://img.shields.io/badge/SharePoint-0078D4?style=for-the-badge&logo=microsoft-sharepoint&logoColor=white" />

<br /><br />

# 🏥 Apollo Healthcare Management Platform

**An enterprise-grade Dynamics 365 CE & Power Platform solution for end-to-end healthcare orchestration.**

*Managing the full patient lifecycle — from admission and lab workflows to automated clinical billing and Azure-powered integrations.*

</div>

---

## 📋 Project Overview

The Apollo Healthcare Management Platform is a custom-engineered CRM solution built on **Microsoft Dynamics 365 CE** and **Dataverse**. It eliminates fragmentation between medical departments by automating the complete patient journey through C# plugins, Custom APIs, Power Automate, and a layered Azure integration stack.

| Area | Details |
|------|---------|
| **Platform** | Microsoft Dynamics 365 CE, Dataverse |
| **Pro-Code** | C# Plugins, Custom API, Custom Actions, JavaScript |
| **Automation** | Power Automate, Classic Workflows, Azure Logic Apps |
| **Azure** | Azure Service Bus, Azure Functions, Azure Logic Apps |
| **Integration** | SharePoint, WhatsApp API, OData / REST API |
| **UI Extensions** | Custom Pages, PCF Controls, Ribbon Workbench, Subgrids |
| **Data Layer** | Virtual Tables, FetchXML, OData, Entity Modeling |

---

## ⚡ Custom API — Deep Dive

> The most technically complex component of this project. Custom APIs provide a **named, reusable, permission-controlled server-side endpoint** inside Dataverse — far more powerful and structured than a plugin alone.

### Why Custom API instead of a Plugin?

A standard plugin fires on a fixed message (Create/Update/Delete). A **Custom API** defines a *custom message* with typed input/output parameters, making it callable on-demand from multiple consumers simultaneously — without duplicating logic.

### How it was built

```
┌─────────────────────────────────────────────────────────┐
│                   Custom API Definition                  │
│  Name:    apollo_CalculateBilling                        │
│  Binding: Global (not entity-bound)                      │
│  Request Parameters:  PatientId (EntityReference)        │
│                       AdmissionDate (DateTime)           │
│                       TreatmentCodes (String)            │
│  Response Parameters: TotalAmount (Decimal)              │
│                       InvoiceId (EntityReference)        │
│                       StatusMessage (String)             │
└─────────────────────────────────────────────────────────┘
                          │
              Backed by a C# Plugin class
              implementing IPlugin — executes
              billing calculation & record creation
```

### Called from 3 different contexts

**1. From JavaScript (Ribbon button click)**
```javascript
var request = {
    PatientId: { id: patientId, entityType: "apollo_patient" },
    AdmissionDate: admissionDate,
    TreatmentCodes: treatmentCodes,
    getMetadata: function () {
        return {
            boundParameter: null,
            parameterTypes: {
                "PatientId":       { typeName: "mscrm.apollo_patient", structuralProperty: 5 },
                "AdmissionDate":   { typeName: "Edm.DateTimeOffset",   structuralProperty: 1 },
                "TreatmentCodes":  { typeName: "Edm.String",           structuralProperty: 1 }
            },
            operationType: 0,
            operationName: "apollo_CalculateBilling"
        };
    }
};

Xrm.WebApi.online.execute(request).then(function (response) {
    return response.json();
}).then(function (result) {
    // Typed response returned directly — update form fields
    formContext.getAttribute("apollo_totalamount").setValue(result.TotalAmount);
    formContext.getAttribute("apollo_invoiceid").setValue(result.InvoiceId);
    Xrm.Navigation.openAlertDialog({ text: result.StatusMessage });
});
```

**2. From Power Automate**
```
Trigger: When patient BPF reaches "Billing" stage
  │
  └─► Dataverse — Perform an unbound action
        Action Name:    apollo_CalculateBilling
        PatientId:      @{triggerBody()?['apollo_patientid']}
        AdmissionDate:  @{triggerBody()?['createdon']}
        TreatmentCodes: @{variables('TreatmentCodesList')}
  │
  └─► Response: TotalAmount, InvoiceId, StatusMessage
        └─► Send email to patient with invoice details
```

**3. From Ribbon Button (Ribbon Workbench)**
- Command bar button configured with enable/display rules
- Triggers the JavaScript caller above on click
- Shows feedback to user using returned `StatusMessage` response parameter

### Why this pattern matters
Unlike a plugin that fires silently on CRUD operations, this Custom API:
- ✅ Returns **typed response data** back to the caller (JS, flow, or external system)
- ✅ Is callable via the **OData endpoint** by any external system with Dataverse auth
- ✅ Has its own **privilege** in the security model — role-based access on who can execute it
- ✅ Is **reusable** — one definition, three consumers, zero duplicated business logic

---

## ☁️ Azure Integration

### Azure Functions — Multiple Responsibilities

Azure Functions handle lightweight, stateless processing tasks between D365 and external systems across three trigger patterns:

```
┌──────────────┐    HTTP Trigger     ┌──────────────────────┐
│  D365 / Flow │ ──────────────────► │   Azure Function      │
│              │                     │  · Transform payload  │
│              │ ◄────────────────── │  · Call external API  │
└──────────────┘   Structured JSON   │  · Return response    │
                                     └──────────────────────┘

┌─────────────────┐  Service Bus Msg  ┌──────────────────────┐
│  Azure Service  │ ────────────────► │   Azure Function      │
│  Bus Queue      │                   │  (Service Bus Trigger)│
└─────────────────┘                   │  · Parse message      │
                                      │  · Write to D365      │
                                      └──────────────────────┘
```

**Function 1 — HTTP Trigger: Data Transformation**

Receives a raw D365 payload via Power Automate HTTP action, maps and transforms field names and data formats to match the external lab system's schema, and returns the cleaned payload. This keeps transformation logic out of Power Automate expressions and in maintainable, testable code.

**Function 2 — HTTP Trigger: External API Proxy**

Acts as a secure proxy so third-party API keys never need to be stored inside Dataverse or Power Automate. D365 calls the function → the function authenticates and calls the external endpoint → returns the result back to the flow.

**Function 3 — Service Bus Trigger: Inbound Message Processor**

Listens on the Azure Service Bus queue. When an external system publishes a message (e.g. lab results), this function parses the payload and writes the data back into the corresponding D365 patient record via the Dataverse Web API.

---

### Azure Service Bus

Configured an **Azure Service Bus namespace and queue** as the messaging backbone for reliable async communication between D365 and external systems. The queue acts as a durable buffer — messages are never lost even if either side is temporarily unavailable.

```
D365 (Power Automate)
        │
        │  Send message on record event
        ▼
  Service Bus Queue  ──►  Azure Function (SB Trigger)  ──►  Process & write to D365
        │
  Decouples sender from receiver
  Guaranteed delivery, retry on failure
```

Setup included:
- Namespace and queue provisioning in Azure Portal
- Shared Access Signature (SAS) connection string configuration
- Power Automate Service Bus connector sending messages on D365 record events
- Azure Function consuming and processing queue messages

---

### Azure Logic Apps

Used to orchestrate **multi-step integration workflows** for scenarios requiring branching across multiple external endpoints — situations where Power Automate alone would become difficult to manage and debug at scale.

---

## 🏗️ Core D365 Development

<table>
  <tr>
    <td width="50%" valign="top">

### 🔩 Plugin Development
- **Pre-Operation plugins** — Validate and enrich patient data before it hits the database. Throw early to prevent bad records without a rollback cost.
- **Post-Operation plugins** — Trigger downstream record creation (billing, appointments) after a patient record is saved.
- **Async plugins** — Background notification dispatch without blocking the user.

    </td>
    <td width="50%" valign="top">

### ⚙️ Automation Stack
- **Classic Workflows** — Synchronous instant-execution for real-time field updates where Power Automate's async nature is unsuitable.
- **Power Automate** — External integrations, approval loops, email templating, scheduled reports.
- **Business Rules** — Declarative client/server validation at each BPF stage — no code, enforced at both UI and server level.

    </td>
  </tr>
</table>

---

## 🧬 Dataverse & UI Extensibility

### Virtual Tables
External healthcare data surfaced as native D365 records without physical migration. Users interact with external system data directly inside forms, views, and relationships as if it were standard Dataverse data.

### Custom Pages & PCF Controls
- **Custom Pages** — Power Apps-based modern UI embedded inside the model-driven app. Role-specific dashboards built for doctors vs. admin staff with different layouts and data scopes.
- **PCF Controls** — Custom Power Components Framework controls replacing standard field rendering with enhanced visual components.
- **Ribbon Workbench** — Command bar buttons with enable/display rules wired to JavaScript and Custom API calls.

### SharePoint Document Management
SharePoint libraries connected to D365 patient entities. Each patient record automatically gets a linked SharePoint folder on creation. Lab reports, prescriptions, and discharge summaries are accessible directly from the D365 form without leaving the app.

---

## 🔐 Security Architecture

```
Organisation
    └── Business Units (Hospital / Billing / Lab)
            └── Teams (Doctors, Admin, Billing)
                    └── Security Roles (entity-level privileges per team)
                            └── Field-Level Security Profiles
                                (diagnosis fields, billing amounts)
```

Custom API execution is also secured via its own privilege — only roles explicitly granted `apollo_CalculateBilling` execute access can invoke it, regardless of their entity-level permissions.

---

## 📸 System Gallery

<div align="center">
<table border="0" cellspacing="8">
  <tr>
    <td align="center">
      <img src="https://cdn.discordapp.com/attachments/933790279528484947/1493172552212680755/image.png?ex=69de00ce&is=69dcaf4e&hm=0a506ad23e778298f1fd5a0a1b6d7e723ad9267bd5d661ef11adbbf0f30b3beb" width="210" />
      <br /><sub><b>Patient Dashboard</b></sub>
    </td>
    <td align="center">
      <img src="https://cdn.discordapp.com/attachments/933790279528484947/1493172897295110245/image.png?ex=69de0120&is=69dcafa0&hm=78bf686e7f168dd692a01dcd926840b3d47f34bbe21d69080074e78c7b742adf" width="210" />
      <br /><sub><b>Billing Logic</b></sub>
    </td>
    <td align="center">
      <img src="https://cdn.discordapp.com/attachments/933790279528484947/1493172855075111055/image.png?ex=69de0116&is=69dcaf96&hm=951256d7ecacba70744bf81aeb6d61212a9b877f148d1def273af75a008a20fa" width="210" />
      <br /><sub><b>Patient BPF Flow</b></sub>
    </td>
    <td align="center">
      <img src="https://cdn.discordapp.com/attachments/933790279528484947/1493173077381742754/image.png?ex=69de014b&is=69dcafcb&hm=7224cae53cf5d95f8b5f450ca61350bf5cb94a1c639c42ac3c9a6f2c992d5264" width="210" />
      <br /><sub><b>Custom Page Popup</b></sub>
    </td>
  </tr>
</table>
</div>

---

## 📂 Repository Structure

```bash
Apollo-HealthCare-Management-D365-CRM/
│
├── 📦 Solutions/                   # Managed & Unmanaged exported D365 solutions
│
├── 📜 WebResources/                # JavaScript, HTML, CSS
│   ├── JS/                         # Form scripts, Xrm.WebApi calls, Custom API callers
│   └── HTML/                       # Custom web resource pages
│
├── ⚙️ Plugins/                     # C# Plugin source code
│   ├── BillingPlugin.cs            # Custom API backing plugin — billing logic
│   ├── PatientValidationPlugin.cs  # Pre-op validation plugin
│   └── PluginRegistration.png      # Plugin Registration Tool steps screenshot
│
├── 🔌 CustomAPI/                   # Custom API & Custom Action definitions
│   └── apollo_CalculateBilling/    # Request params, response params, plugin binding
│
├── 🤖 Workflows/                   # Automation exports
│   ├── PowerAutomate/              # Cloud flow exports (.zip)
│   └── ClassicWorkflows/           # Legacy workflow XML exports
│
├── ☁️ AzureIntegration/            # Azure components
│   ├── Functions/                  # Azure Function source code (HTTP + SB triggers)
│   ├── LogicApps/                  # Logic App workflow definitions
│   └── ServiceBus/                 # Queue config & SAS setup notes
│
└── 📖 README.md
```

---

## 🎯 Skills Demonstrated

| Category | Skills |
|----------|--------|
| **Custom Development** | Custom API, Custom Actions, C# Plugins (Pre/Post/Async), JavaScript |
| **Azure** | Azure Functions (HTTP + Service Bus Trigger), Service Bus, Logic Apps |
| **Power Platform** | Power Automate, Custom Pages, PCF Controls, Business Process Flows, Business Rules |
| **Dataverse** | Virtual Tables, FetchXML, OData, REST API, Entity & Relationship Modeling |
| **UI Customization** | Ribbon Workbench, Custom Pages, PCF Controls, Subgrids, Form Design |
| **Integration** | SharePoint, WhatsApp API, Azure-D365 messaging, External API proxy pattern |
| **Security** | Security Roles, Field-Level Security, Teams, Business Units, API-level privileges |
| **ALM** | Managed/Unmanaged Solutions, Solution Layering, Multi-environment Deployment |

---

## 👩‍💻 About the Developer

Built by **Priyadarshini** — Microsoft Dynamics 365 CRM & Power Platform Developer with 3 years of experience at Ecolab Digital Center, Bangalore.

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/LinkedIn_Priya)
[![Portfolio](https://img.shields.io/badge/Portfolio-000000?style=for-the-badge&logo=About.me&logoColor=white)](https://Priya_Portfolio)

> 💡 *Personal project built to demonstrate enterprise-level D365 CE development patterns — Custom API architecture, Azure integration, advanced Dataverse extensibility, and full Power Platform coverage.*
