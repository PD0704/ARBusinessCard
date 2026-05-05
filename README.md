# AR Business Card

An AR-powered business card app built with Unity 2022.3 LTS. Scan a physical business card to see the owner's full profile in augmented reality — name, role, contact info, LinkedIn, portfolio, and resume.

## Demo
> Demo GIF coming soon after first device build

## Features
- Dual scan mode — AR image tracking (Vuforia Cloud) + QR code fallback
- Floating AR overlay anchored to the physical card in 3D space
- Save contact directly to device, open LinkedIn, view PDF resume
- Creator flow — fill profile, generate card as downloadable PNG
- Offline cache — works without internet if profile was fetched before
- Firebase Auth — email/password login and registration
- Secure — all keys excluded from Git, Firestore rules enforced

## Tech Stack
|      Layer      |                      Technology                         |
|-----------------|---------------------------------------------------------|
|   AR Runtime    | Unity 2022.3 LTS + AR Foundation 5.1.4                  |
| Image Tracking  | Vuforia Engine 11.4.4 Cloud Recognition                 |
|   QR Scanning   | ZXing.Net                                               |
|     Backend     | Firebase (Auth + Firestore + Storage + Cloud Functions) |
|    Platform     | Android (ARM64, API 26+)                                |

## Architecture
Physical Card → Camera → Vuforia Cloud Match → UID → Firebase Fetch → AR Overlay
Physical Card → Camera → ZXing QR Decode → UID → Firebase Fetch → Flat UI

## Project Structure
ARBusinessCard/
├── Assets/
│   ├── Scripts/
│   │   ├── Models/          # UserProfile data model
│   │   ├── Config/          # AppConfig ScriptableObject
│   │   ├── ARSessionManager.cs
│   │   ├── VuforiaCloudManager.cs
│   │   ├── QRScanner.cs
│   │   ├── QRGenerator.cs
│   │   ├── ScanModeController.cs
│   │   ├── FirebaseManager.cs
│   │   ├── ProfileService.cs
│   │   ├── ProfileCache.cs
│   │   ├── AROverlayController.cs
│   │   ├── ProfileCardUI.cs
│   │   ├── ContactSaver.cs
│   │   ├── CardGenerator.cs
│   │   ├── CardTemplateUI.cs
│   │   ├── AppStateManager.cs
│   │   ├── AuthManager.cs
│   │   ├── AuthUI.cs
│   │   ├── HomeUI.cs
│   │   └── ScanUI.cs
│   ├── Prefabs/
│   │   ├── AROverlayCanvas.prefab
│   │   └── CardTemplateCanvas.prefab
│   └── Plugins/
│       └── zxing.dll
├── functions/               # Firebase Cloud Functions
│   ├── index.js
│   └── vuforiaClient.js
├── firestore.rules
├── storage.rules
└── firebase.json

## Setup
1. Clone the repo
2. Open in Unity 2022.3 LTS
3. Install packages via Package Manager:
   - AR Foundation 5.1.4
   - ARCore XR Plugin 5.1.4
4. Import Firebase Unity SDK (Auth, Firestore, Storage)
5. Import Vuforia Engine 11.x unitypackage
6. Create `Assets/Resources/AppConfig.asset` from the AppConfig ScriptableObject
7. Fill in your Vuforia and Firebase keys in AppConfig
8. Add your `google-services.json` to `Assets/`
9. Build for Android (ARM64, API 26+)

## Security
All API keys and credentials are excluded from this repository via `.gitignore`.
See `.env.example` and `AppConfig.cs` for required configuration.

## Author
Prahelika Dutta — Immersive Technology Developer
[LinkedIn](#) · [Portfolio](#)