# Security

Merchant holds a shop's sales and takes part in a local network, so security reports are taken seriously.

## Reporting a vulnerability

Report it privately through GitHub: open the repository's **Security** tab and choose **Report a vulnerability**, or go to https://github.com/rizonesoft/Merchant/security/advisories/new. Please do not open a public issue or pull request for it.

Include what you found, how to reproduce it, and what an attacker could do with it. You will get a reply within seven days. Once a fix is released the report is published with credit to you, unless you ask otherwise.

## What is in scope

- The sync protocol between tills: pairing, certificate pinning and the three sync endpoints (`src/Merchant.Core/Sync`, `src/Merchant.SyncHost`).
- Encryption of the database and of backups (`src/Merchant.Core/Storage`).
- Anything that lets a device without the pairing code read or write a shop's data.
- Anything that loses or silently changes a recorded sale.

## What to know about the design

- The pairing code is the key to a shop. Anyone who holds it can join the shop and read and write its data. Keep it private.
- The database is encrypted with a key kept on the till: sealed with the Windows account on Windows, and in a file only the owner can read on Linux. Someone who can run programs as that user can read the data.
- Tills talk only on the local network. Merchant makes no connection to the internet.

## Supported versions

Merchant has not had a stable release yet. Fixes go to the `main` branch.
